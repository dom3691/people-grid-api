using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Approvals.DTOs;

public sealed record ApprovalFlowListQuery(Guid? RequestTypeId, Guid? DepartmentId, bool? IsActive, int PageNumber = 1, int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record CreateApprovalFlowRequest(string Name, Guid? RequestTypeId, Guid? DepartmentId, IReadOnlyCollection<ApprovalFlowStepRequest> Steps);
public sealed record UpdateApprovalFlowRequest(string Name, Guid? RequestTypeId, Guid? DepartmentId, IReadOnlyCollection<ApprovalFlowStepRequest> Steps);
public sealed record ApprovalFlowStepRequest(int Sequence, string ApproverType, Guid? ApproverRoleId, Guid? ApproverUserId, int? SlaHours);
public sealed record ChangeApprovalFlowStatusRequest(bool IsActive);
public sealed record CreateApprovalInstanceRequest(Guid RequestId);
public sealed record ApprovalDecisionRequest(string? Comments);
public sealed record EscalateApprovalRequest(Guid EscalatedToUserId, string Reason);

public sealed record ApprovalFlowDto(Guid Id, string Name, Guid? RequestTypeId, string? RequestType, Guid? DepartmentId, string? Department, bool IsActive, IReadOnlyCollection<ApprovalFlowStepDto> Steps);
public sealed record ApprovalFlowStepDto(Guid Id, int Sequence, string ApproverType, Guid? ApproverRoleId, string? ApproverRole, Guid? ApproverUserId, string? ApproverUser, int? SlaHours, bool IsActive);
public sealed record ApprovalInstanceDto(Guid Id, Guid RequestId, string Status, Guid? CurrentStepId, DateTime StartedAt, DateTime? CompletedAt);
public sealed record PendingApprovalDto(Guid ApprovalInstanceStepId, Guid ApprovalInstanceId, Guid RequestId, string RequestNumber, string Subject, string Status, DateTime AssignedAt, int? SlaHours);
public sealed record ApprovalHistoryDto(Guid Id, string Decision, string? Comments, Guid ActorUserId, DateTime DecidedAt, int StepSequence);
