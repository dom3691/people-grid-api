using PeopleGrid.Application.Features.AuditLogs.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.AuditLogs.Interfaces;

public interface IAuditLogService
{
    Task<PaginatedResponse<AuditLogListItemDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuditLogExportDto> ExportAsync(AuditLogExportQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogSummaryDto> GetSummaryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default);
}
