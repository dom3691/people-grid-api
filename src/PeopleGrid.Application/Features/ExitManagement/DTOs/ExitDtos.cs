using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.ExitManagement.DTOs;

public sealed record ExitCaseQuery(string? Status, Guid? DepartmentId, DateOnly? FromDate, DateOnly? ToDate, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record SubmitResignationRequest(Guid EmployeeId, DateOnly ResignationDate, DateOnly ProposedLastWorkingDay, string Reason, int NoticePeriod = 30, bool HrOverride = false);
public sealed record ExitDecisionRequest(string? Comments);
public sealed record AddExitClearanceItemRequest(string ItemName, Guid? OwnerUserId, bool IsMandatory = true);
public sealed record CompleteExitClearanceItemRequest(string? Comments);
public sealed record ExitHandoverRequest(Guid? HandoverToUserId, string Notes);
public sealed record ExitInterviewRequest(IReadOnlyCollection<ExitInterviewAnswerRequest> Answers);
public sealed record ExitInterviewAnswerRequest(string Question, string? Response);
public sealed record UpdateFinalSettlementStatusRequest(string Status, string? Comments);

public sealed record ExitCaseDto(
    Guid Id,
    string CaseNumber,
    Guid EmployeeId,
    DateOnly ResignationDate,
    DateOnly LastWorkingDay,
    string? Reason,
    int NoticePeriod,
    string Status,
    int MandatoryClearanceItems,
    int CompletedMandatoryClearanceItems);

