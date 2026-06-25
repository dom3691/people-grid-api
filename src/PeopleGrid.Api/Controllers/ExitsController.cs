using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.ExitManagement.DTOs;
using PeopleGrid.Application.Features.ExitManagement.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/exits")]
[Authorize]
public sealed class ExitsController(IExitManagementService exitService) : ControllerBase
{
    [HttpPost("resignations")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Submit(SubmitResignationRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.SubmitResignationAsync(request, ct), "Resignation submitted"));
    [HttpGet][HasPermission("Exit.View")] public async Task<ActionResult<ApiResponse<PaginatedResponse<ExitCaseDto>>>> List([FromQuery] ExitCaseQuery query, CancellationToken ct) => Ok(ApiResponse<PaginatedResponse<ExitCaseDto>>.Ok(await exitService.ListAsync(query, ct)));
    [HttpGet("{id:guid}")][HasPermission("Exit.View")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Detail(Guid id, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.GetAsync(id, ct)));
    [HttpPost("{id:guid}/approve")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Approve(Guid id, ExitDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.ApproveAsync(id, request, ct), "Exit approved"));
    [HttpPost("{id:guid}/reject")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Reject(Guid id, ExitDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.RejectAsync(id, request, ct), "Exit rejected"));
    [HttpPost("{id:guid}/clearance-items")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> AddClearance(Guid id, AddExitClearanceItemRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.AddClearanceItemAsync(id, request, ct), "Clearance item added"));
    [HttpPatch("clearance-items/{id:guid}/complete")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> CompleteClearance(Guid id, CompleteExitClearanceItemRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.CompleteClearanceItemAsync(id, request, ct), "Clearance item completed"));
    [HttpPost("{id:guid}/handover")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Handover(Guid id, ExitHandoverRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.RecordHandoverAsync(id, request, ct), "Handover recorded"));
    [HttpPost("{id:guid}/exit-interview")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Interview(Guid id, ExitInterviewRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.SubmitExitInterviewAsync(id, request, ct), "Exit interview submitted"));
    [HttpPatch("{id:guid}/final-settlement-status")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Settlement(Guid id, UpdateFinalSettlementStatusRequest request, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.UpdateFinalSettlementStatusAsync(id, request, ct), "Final settlement updated"));
    [HttpPost("{id:guid}/deactivate-account")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Deactivate(Guid id, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.DeactivateAccountAsync(id, ct), "Account deactivated"));
    [HttpPost("{id:guid}/close")][HasPermission("Exit.Manage")] public async Task<ActionResult<ApiResponse<ExitCaseDto>>> Close(Guid id, CancellationToken ct) => Ok(ApiResponse<ExitCaseDto>.Ok(await exitService.CloseAsync(id, ct), "Exit case closed"));
}

