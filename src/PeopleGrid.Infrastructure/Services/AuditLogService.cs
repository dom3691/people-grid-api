using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.AuditLogs.DTOs;
using PeopleGrid.Application.Features.AuditLogs.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class AuditLogService(IApplicationDbContext dbContext) : IAuditLogService
{
    private const int MaxExportRows = 10000;
    private static readonly string[] SensitiveFieldMarkers =
    [
        "password", "token", "secret", "privatekey", "connectionstring",
        "accountnumber", "bankaccount", "salary", "pin"
    ];

    public async Task<PaginatedResponse<AuditLogListItemDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var auditLogs = ApplyFilters(BaseQuery(), query).OrderByDescending(x => x.Timestamp);
        var totalCount = await auditLogs.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var logs = await auditLogs.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var actorNames = await LoadActorNamesAsync(logs.Select(x => x.ActorUserId), cancellationToken);
        var items = logs.Select(x => MapListItem(x, actorNames)).ToList();
        return new PaginatedResponse<AuditLogListItemDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<AuditLogDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var auditLog = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Audit log was not found.");
        var actorNames = await LoadActorNamesAsync([auditLog.ActorUserId], cancellationToken);
        return MapDetails(auditLog, actorNames);
    }

    public async Task<AuditLogExportDto> ExportAsync(AuditLogExportQuery query, CancellationToken cancellationToken = default)
    {
        var auditQuery = new AuditLogQuery(
            query.Search,
            query.ActorUserId,
            query.DateFrom,
            query.DateTo,
            query.Module,
            query.Action,
            query.EntityType,
            query.EntityId,
            query.Severity,
            query.Outcome,
            query.CorrelationId,
            1,
            MaxExportRows);

        var logs = await ApplyFilters(BaseQuery(), auditQuery)
            .OrderByDescending(x => x.Timestamp)
            .Take(MaxExportRows)
            .ToListAsync(cancellationToken);

        var actorNames = await LoadActorNamesAsync(logs.Select(x => x.ActorUserId), cancellationToken);
        var normalizedFormat = query.Format.Trim().ToLowerInvariant();
        var isExcel = normalizedFormat is "excel" or "xlsx";
        var separator = isExcel ? "\t" : ",";
        var content = BuildDelimitedExport(logs, actorNames, separator);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        var extension = isExcel ? "xls" : "csv";
        var contentType = isExcel ? "application/vnd.ms-excel" : "text/csv";
        return new AuditLogExportDto(bytes, contentType, $"audit-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}");
    }

    public async Task<AuditLogSummaryDto> GetSummaryAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var auditLogs = ApplyFilters(BaseQuery().AsNoTracking(), query);
        var totalCount = await auditLogs.CountAsync(cancellationToken);
        var successCount = await auditLogs.CountAsync(x => x.Outcome == "Success", cancellationToken);
        var failureCount = totalCount - successCount;
        var byModule = await auditLogs.GroupBy(x => x.Module).OrderByDescending(x => x.Count()).Take(20).Select(x => new AuditSummaryBucketDto(x.Key, x.Count())).ToListAsync(cancellationToken);
        var bySeverity = await auditLogs.GroupBy(x => x.Severity).OrderByDescending(x => x.Count()).Select(x => new AuditSummaryBucketDto(x.Key, x.Count())).ToListAsync(cancellationToken);
        var byOutcome = await auditLogs.GroupBy(x => x.Outcome).OrderByDescending(x => x.Count()).Select(x => new AuditSummaryBucketDto(x.Key, x.Count())).ToListAsync(cancellationToken);
        var retentionDays = await GetRetentionDaysAsync(cancellationToken);

        return new AuditLogSummaryDto(totalCount, successCount, failureCount, byModule, bySeverity, byOutcome, retentionDays);
    }

    public async Task<AuditLogLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var modules = await dbContext.AuditLogs.AsNoTracking().Select(x => x.Module).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var actions = await dbContext.AuditLogs.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var entityTypes = await dbContext.AuditLogs.AsNoTracking().Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var severities = await dbContext.AuditLogs.AsNoTracking().Select(x => x.Severity).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var outcomes = await dbContext.AuditLogs.AsNoTracking().Select(x => x.Outcome).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var actorIds = await dbContext.AuditLogs.AsNoTracking()
            .Where(x => x.ActorUserId != null)
            .Select(x => x.ActorUserId!)
            .Distinct()
            .OrderBy(x => x)
            .Take(500)
            .ToListAsync(cancellationToken);
        var actorNames = await LoadActorNamesAsync(actorIds, cancellationToken);
        var actors = actorIds.Select(x => new AuditActorLookupDto(x, actorNames.GetValueOrDefault(x, x))).ToList();

        return new AuditLogLookupsDto(modules, actions, entityTypes, severities, outcomes, actors);
    }

    private IQueryable<AuditLog> BaseQuery()
    {
        return dbContext.AuditLogs.AsNoTracking().Include(x => x.Detail);
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> auditLogs, AuditLogQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            auditLogs = auditLogs.Where(x =>
                x.Module.ToLower().Contains(search) ||
                x.Action.ToLower().Contains(search) ||
                x.EntityType.ToLower().Contains(search) ||
                (x.EntityId != null && x.EntityId.ToLower().Contains(search)) ||
                (x.ActorUserId != null && x.ActorUserId.ToLower().Contains(search)) ||
                (x.CorrelationId != null && x.CorrelationId.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
        {
            auditLogs = auditLogs.Where(x => x.ActorUserId == query.ActorUserId.Trim());
        }
        if (query.DateFrom is not null)
        {
            auditLogs = auditLogs.Where(x => x.Timestamp >= query.DateFrom.Value);
        }
        if (query.DateTo is not null)
        {
            auditLogs = auditLogs.Where(x => x.Timestamp <= query.DateTo.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            auditLogs = auditLogs.Where(x => x.Module == query.Module.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditLogs = auditLogs.Where(x => x.Action == query.Action.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            auditLogs = auditLogs.Where(x => x.EntityType == query.EntityType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            auditLogs = auditLogs.Where(x => x.EntityId == query.EntityId.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            auditLogs = auditLogs.Where(x => x.Severity == query.Severity.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            auditLogs = auditLogs.Where(x => x.Outcome == query.Outcome.Trim());
        }
        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            auditLogs = auditLogs.Where(x => x.CorrelationId == query.CorrelationId.Trim());
        }

        return auditLogs;
    }

    private async Task<Dictionary<string, string>> LoadActorNamesAsync(IEnumerable<string?> actorUserIds, CancellationToken cancellationToken)
    {
        var ids = actorUserIds
            .Select(x => Guid.TryParse(x, out var id) ? id : (Guid?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        var users = await dbContext.Users.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            x => x.Id.ToString(),
            x => string.IsNullOrWhiteSpace($"{x.FirstName} {x.LastName}".Trim()) ? x.Email : $"{x.FirstName} {x.LastName}".Trim());
    }

    private static AuditLogListItemDto MapListItem(AuditLog auditLog, IReadOnlyDictionary<string, string> actorNames)
    {
        var actor = NormalizeActor(auditLog.ActorUserId);
        return new AuditLogListItemDto(
            auditLog.Id,
            auditLog.Timestamp,
            actor,
            ResolveActorName(actor, actorNames),
            auditLog.Module,
            auditLog.Action,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.Outcome,
            auditLog.Severity,
            auditLog.IpAddress,
            auditLog.CorrelationId);
    }

    private static AuditLogDetailsDto MapDetails(AuditLog auditLog, IReadOnlyDictionary<string, string> actorNames)
    {
        var actor = NormalizeActor(auditLog.ActorUserId);
        return new AuditLogDetailsDto(
            auditLog.Id,
            auditLog.Timestamp,
            actor,
            ResolveActorName(actor, actorNames),
            auditLog.Module,
            auditLog.Action,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.Outcome,
            auditLog.Severity,
            auditLog.IpAddress,
            auditLog.UserAgent,
            auditLog.CorrelationId,
            auditLog.RetentionUntil,
            auditLog.ArchivedAt,
            auditLog.PartitionKey,
            MaskJson(auditLog.Detail?.OldValuesJson),
            MaskJson(auditLog.Detail?.NewValuesJson),
            MaskJson(auditLog.Detail?.ChangedFieldsJson));
    }

    private static string BuildDelimitedExport(IReadOnlyCollection<AuditLog> logs, IReadOnlyDictionary<string, string> actorNames, string separator)
    {
        var builder = new StringBuilder();
        WriteRow(builder, separator, ["Timestamp", "Actor", "Actor Name", "Module", "Action", "Entity Type", "Entity Id", "Outcome", "Severity", "IP Address", "Correlation Id", "Old Values", "New Values"]);
        foreach (var log in logs)
        {
            var actor = NormalizeActor(log.ActorUserId);
            WriteRow(builder, separator,
            [
                log.Timestamp.ToString("O"),
                actor,
                ResolveActorName(actor, actorNames),
                log.Module,
                log.Action,
                log.EntityType,
                log.EntityId ?? string.Empty,
                log.Outcome,
                log.Severity,
                log.IpAddress ?? string.Empty,
                log.CorrelationId ?? string.Empty,
                MaskJson(log.Detail?.OldValuesJson) ?? string.Empty,
                MaskJson(log.Detail?.NewValuesJson) ?? string.Empty
            ]);
        }

        return builder.ToString();
    }

    private static void WriteRow(StringBuilder builder, string separator, IReadOnlyCollection<string> values)
    {
        builder.AppendLine(string.Join(separator, values.Select(x => EscapeDelimitedValue(x, separator))));
    }

    private static string EscapeDelimitedValue(string value, string separator)
    {
        var escaped = value.Replace("\"", "\"\"");
        return escaped.Contains(separator) || escaped.Contains('\n') || escaped.Contains('\r') || escaped.Contains('"')
            ? $"\"{escaped}\""
            : escaped;
    }

    private async Task<int> GetRetentionDaysAsync(CancellationToken cancellationToken)
    {
        var raw = await dbContext.SystemParameters.AsNoTracking()
            .Where(x => x.Key == "Audit.RetentionDays")
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return int.TryParse(raw, out var value) && value > 0 ? value : 365;
    }

    private static string NormalizeActor(string? actorUserId)
    {
        return string.IsNullOrWhiteSpace(actorUserId) ? "SYSTEM" : actorUserId;
    }

    private static string ResolveActorName(string actorUserId, IReadOnlyDictionary<string, string> actorNames)
    {
        return actorUserId == "SYSTEM" ? "System / Service" : actorNames.GetValueOrDefault(actorUserId, actorUserId);
    }

    private static string? MaskJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            MaskNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void MaskNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (property.Value is null)
                {
                    continue;
                }

                if (IsSensitiveField(property.Key))
                {
                    obj[property.Key] = "********";
                    continue;
                }

                MaskNode(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array.Where(x => x is not null))
            {
                MaskNode(item!);
            }
        }
    }

    private static bool IsSensitiveField(string fieldName)
    {
        var normalized = fieldName.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        return SensitiveFieldMarkers.Any(normalized.Contains);
    }
}
