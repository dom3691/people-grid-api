using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Approvals.DTOs;
using PeopleGrid.Application.Features.Approvals.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ApprovalsController(IApprovalWorkflowService approvalWorkflowService) : ControllerBase
{
    [HttpPost("api/approval-flows")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> CreateFlow(CreateApprovalFlowRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.CreateFlowAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetFlowById), new { id = response.Id }, ApiResponse<ApprovalFlowDto>.Ok(response, "Approval flow created successfully"));
    }

    [HttpGet("api/approval-flows")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ApprovalFlowDto>>>> ListFlows([FromQuery] ApprovalFlowListQuery query, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.ListFlowsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<ApprovalFlowDto>>.Ok(response));
    }

    [HttpGet("api/approval-flows/{id:guid}")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> GetFlowById(Guid id, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.GetFlowByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ApprovalFlowDto>.Ok(response));
    }

    [HttpPut("api/approval-flows/{id:guid}")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> UpdateFlow(Guid id, UpdateApprovalFlowRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.UpdateFlowAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ApprovalFlowDto>.Ok(response, "Approval flow updated successfully"));
    }

    [HttpPatch("api/approval-flows/{id:guid}/status")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalFlowDto>>> ChangeFlowStatus(Guid id, ChangeApprovalFlowStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.ChangeFlowStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ApprovalFlowDto>.Ok(response, "Approval flow status updated successfully"));
    }

    [HttpPost("api/approval-instances")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<ApprovalInstanceDto>>> CreateInstance(CreateApprovalInstanceRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.CreateInstanceAsync(request, cancellationToken);
        return Ok(ApiResponse<ApprovalInstanceDto>.Ok(response, "Approval instance created successfully"));
    }

    [HttpGet("api/approvals/pending")]
    [HasPermission("Approval.Approve")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PendingApprovalDto>>>> Pending(CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.ListPendingAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PendingApprovalDto>>.Ok(response));
    }

    [HttpPost("api/approvals/{id:guid}/approve")]
    [HasPermission("Approval.Approve")]
    public async Task<ActionResult<ApiResponse<ApprovalInstanceDto>>> Approve(Guid id, ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.ApproveAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ApprovalInstanceDto>.Ok(response, "Approval step approved successfully"));
    }

    [HttpPost("api/approvals/{id:guid}/reject")]
    [HasPermission("Approval.Approve")]
    public async Task<ActionResult<ApiResponse<ApprovalInstanceDto>>> Reject(Guid id, ApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ApprovalInstanceDto>.Ok(response, "Approval step rejected successfully"));
    }

    [HttpPost("api/approvals/{id:guid}/escalate")]
    [HasPermission("Approval.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> Escalate(Guid id, EscalateApprovalRequest request, CancellationToken cancellationToken)
    {
        await approvalWorkflowService.EscalateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Approval step escalated successfully"));
    }

    [HttpGet("api/approvals/{id:guid}/history")]
    [HasPermission("Approval.Approve")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ApprovalHistoryDto>>>> History(Guid id, CancellationToken cancellationToken)
    {
        var response = await approvalWorkflowService.GetHistoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ApprovalHistoryDto>>.Ok(response));
    }
}
