using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Leave.DTOs;
using PeopleGrid.Application.Features.Leave.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/leaves")]
[Authorize]
public sealed class LeaveController(ILeaveService leaveService) : ControllerBase
{
    [HttpPost("requests")]
    [HasPermission("Leave.Apply")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Create(CreateLeaveRequestRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.CreateRequestAsync(request, cancellationToken), "Leave request created successfully"));

    [HttpGet("requests")]
    [HasPermission("Leave.View")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LeaveRequestDto>>>> List([FromQuery] LeaveRequestListQuery query, CancellationToken cancellationToken) => Ok(ApiResponse<PaginatedResponse<LeaveRequestDto>>.Ok(await leaveService.ListRequestsAsync(query, cancellationToken)));

    [HttpGet("requests/{id:guid}")]
    [HasPermission("Leave.View")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Detail(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.GetRequestAsync(id, cancellationToken)));

    [HttpPost("requests/{id:guid}/submit")]
    [HasPermission("Leave.Apply")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Submit(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.SubmitAsync(id, cancellationToken), "Leave request submitted"));

    [HttpPost("requests/{id:guid}/approve")]
    [HasPermission("Leave.Approve")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Approve(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.ApproveAsync(id, request, cancellationToken), "Leave request approved"));

    [HttpPost("requests/{id:guid}/reject")]
    [HasPermission("Leave.Approve")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Reject(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.RejectAsync(id, request, cancellationToken), "Leave request rejected"));

    [HttpPost("requests/{id:guid}/cancel")]
    [HasPermission("Leave.Apply")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Cancel(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveRequestDto>.Ok(await leaveService.CancelAsync(id, request, cancellationToken), "Leave request cancelled"));

    [HttpGet("balances/{employeeId:guid}")]
    [HasPermission("Leave.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LeaveBalanceDto>>>> Balances(Guid employeeId, CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<LeaveBalanceDto>>.Ok(await leaveService.GetBalancesAsync(employeeId, cancellationToken)));

    [HttpGet("history/{employeeId:guid}")]
    [HasPermission("Leave.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LeaveHistoryDto>>>> History(Guid employeeId, CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<LeaveHistoryDto>>.Ok(await leaveService.GetHistoryAsync(employeeId, cancellationToken)));

    [HttpGet("calendar")]
    [HasPermission("Leave.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LeaveCalendarItemDto>>>> Calendar([FromQuery] Guid? departmentId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<LeaveCalendarItemDto>>.Ok(await leaveService.GetCalendarAsync(departmentId, fromDate, toDate, cancellationToken)));

    [HttpPost("entitlements")]
    [HasPermission("Leave.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveEntitlementDto>>> CreateEntitlement(LeaveEntitlementRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveEntitlementDto>.Ok(await leaveService.CreateEntitlementAsync(request, cancellationToken), "Leave entitlement created"));

    [HttpGet("entitlements")]
    [HasPermission("Leave.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LeaveEntitlementDto>>>> ListEntitlements([FromQuery] int pageNumber, [FromQuery] int pageSize, CancellationToken cancellationToken) => Ok(ApiResponse<PaginatedResponse<LeaveEntitlementDto>>.Ok(await leaveService.ListEntitlementsAsync(pageNumber <= 0 ? 1 : pageNumber, pageSize <= 0 ? 20 : pageSize, cancellationToken)));

    [HttpPut("entitlements/{id:guid}")]
    [HasPermission("Leave.Manage")]
    public async Task<ActionResult<ApiResponse<LeaveEntitlementDto>>> UpdateEntitlement(Guid id, LeaveEntitlementRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<LeaveEntitlementDto>.Ok(await leaveService.UpdateEntitlementAsync(id, request, cancellationToken), "Leave entitlement updated"));
}
