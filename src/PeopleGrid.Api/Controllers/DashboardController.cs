using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Reports.DTOs;
using PeopleGrid.Application.Features.Reports.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IReportService reportService) : ControllerBase
{
    [HttpGet("cards")]
    [HasPermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DashboardCardDto>>>> Cards(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<DashboardCardDto>>.Ok(await reportService.GetDashboardCardsAsync(ct)));

    [HttpGet("department-headcount")]
    [HasPermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DepartmentHeadcountDto>>>> DepartmentHeadcount(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyCollection<DepartmentHeadcountDto>>.Ok(await reportService.GetDepartmentHeadcountAsync(ct)));

    [HttpGet("pending-approvals")]
    [HasPermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<PendingApprovalsDto>>> PendingApprovals(CancellationToken ct) =>
        Ok(ApiResponse<PendingApprovalsDto>.Ok(await reportService.GetPendingApprovalsAsync(ct)));
}

