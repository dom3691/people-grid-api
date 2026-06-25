using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.AuditLogs.DTOs;
using PeopleGrid.Application.Features.AuditLogs.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Audit.View")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AuditLogListItemDto>>>> List([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var response = await auditLogService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<AuditLogListItemDto>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Audit.View")]
    public async Task<ActionResult<ApiResponse<AuditLogDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AuditLogDetailsDto>.Ok(response));
    }

    [HttpGet("export")]
    [HasPermission("Audit.View")]
    [HasPermission("Audit.Export")]
    public async Task<IActionResult> Export([FromQuery] AuditLogExportQuery query, CancellationToken cancellationToken)
    {
        var response = await auditLogService.ExportAsync(query, cancellationToken);
        return File(response.Content, response.ContentType, response.FileName);
    }

    [HttpGet("summary")]
    [HasPermission("Audit.View")]
    public async Task<ActionResult<ApiResponse<AuditLogSummaryDto>>> Summary([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetSummaryAsync(query, cancellationToken);
        return Ok(ApiResponse<AuditLogSummaryDto>.Ok(response));
    }

    [HttpGet("lookups")]
    [HasPermission("Audit.View")]
    public async Task<ActionResult<ApiResponse<AuditLogLookupsDto>>> Lookups(CancellationToken cancellationToken)
    {
        var response = await auditLogService.GetLookupsAsync(cancellationToken);
        return Ok(ApiResponse<AuditLogLookupsDto>.Ok(response));
    }
}
