using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Disciplinary.DTOs;

public sealed record DisciplinaryCaseQuery(string? Status, Guid? EmployeeId, Guid? DepartmentId, string? Category, DateOnly? FromDate, DateOnly? ToDate, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}
public sealed record CreateDisciplinaryCaseRequest(Guid EmployeeId, DateOnly IncidentDate, string Category, string QueryDetails, DateOnly IssueDate, DateOnly ResponseDueDate);
public sealed record DisciplinaryResponseRequest(string ResponseText);
public sealed record DisciplinaryReviewRequest(string ReviewComments, string Outcome);
public sealed record WarningLetterRequest(string WarningLevel, string LetterContent);
public sealed record SuspensionRequest(DateOnly StartDate, DateOnly EndDate, string Reason);
public sealed record EscalateDisciplinaryRequest(Guid EscalatedTo, string Reason);
public sealed record CloseDisciplinaryCaseRequest(string Comments, string FinalOutcome);
public sealed record DisciplinaryCaseDto(Guid Id, string CaseNumber, Guid EmployeeId, DateOnly IncidentDate, string Category, string QueryDetails, string Status, DateOnly ResponseDueDate, DateOnly IssueDate);
