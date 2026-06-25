using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.AuditLogs.DTOs;

public sealed record AuditLogQuery(
    string? Search,
    string? ActorUserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Module,
    string? Action,
    string? EntityType,
    string? EntityId,
    string? Severity,
    string? Outcome,
    string? CorrelationId,
    int PageNumber = 1,
    int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record AuditLogExportQuery(
    string? Search,
    string? ActorUserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Module,
    string? Action,
    string? EntityType,
    string? EntityId,
    string? Severity,
    string? Outcome,
    string? CorrelationId,
    string Format = "csv");

public sealed record AuditLogListItemDto(
    Guid Id,
    DateTime Timestamp,
    string ActorUserId,
    string ActorDisplayName,
    string Module,
    string Action,
    string EntityType,
    string? EntityId,
    string Outcome,
    string Severity,
    string? IpAddress,
    string? CorrelationId);

public sealed record AuditLogDetailsDto(
    Guid Id,
    DateTime Timestamp,
    string ActorUserId,
    string ActorDisplayName,
    string Module,
    string Action,
    string EntityType,
    string? EntityId,
    string Outcome,
    string Severity,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId,
    DateTime? RetentionUntil,
    DateTime? ArchivedAt,
    int PartitionKey,
    string? OldValuesJson,
    string? NewValuesJson,
    string? ChangedFieldsJson);

public sealed record AuditLogExportDto(byte[] Content, string ContentType, string FileName);

public sealed record AuditLogSummaryDto(
    int TotalCount,
    int SuccessCount,
    int FailureCount,
    IReadOnlyCollection<AuditSummaryBucketDto> ByModule,
    IReadOnlyCollection<AuditSummaryBucketDto> BySeverity,
    IReadOnlyCollection<AuditSummaryBucketDto> ByOutcome,
    int RetentionDays);

public sealed record AuditSummaryBucketDto(string Name, int Count);

public sealed record AuditLogLookupsDto(
    IReadOnlyCollection<string> Modules,
    IReadOnlyCollection<string> Actions,
    IReadOnlyCollection<string> EntityTypes,
    IReadOnlyCollection<string> Severities,
    IReadOnlyCollection<string> Outcomes,
    IReadOnlyCollection<AuditActorLookupDto> Actors);

public sealed record AuditActorLookupDto(string ActorUserId, string DisplayName);
