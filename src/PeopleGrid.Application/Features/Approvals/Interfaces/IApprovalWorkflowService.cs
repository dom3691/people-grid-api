using PeopleGrid.Application.Features.Approvals.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Approvals.Interfaces;

public interface IApprovalWorkflowService
{
    Task<ApprovalFlowDto> CreateFlowAsync(CreateApprovalFlowRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<ApprovalFlowDto>> ListFlowsAsync(ApprovalFlowListQuery query, CancellationToken cancellationToken = default);
    Task<ApprovalFlowDto> GetFlowByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApprovalFlowDto> UpdateFlowAsync(Guid id, UpdateApprovalFlowRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalFlowDto> ChangeFlowStatusAsync(Guid id, ChangeApprovalFlowStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalInstanceDto> CreateInstanceAsync(CreateApprovalInstanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PendingApprovalDto>> ListPendingAsync(CancellationToken cancellationToken = default);
    Task<ApprovalInstanceDto> ApproveAsync(Guid approvalInstanceStepId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalInstanceDto> RejectAsync(Guid approvalInstanceStepId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default);
    Task EscalateAsync(Guid approvalInstanceStepId, EscalateApprovalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ApprovalHistoryDto>> GetHistoryAsync(Guid approvalInstanceId, CancellationToken cancellationToken = default);
}
