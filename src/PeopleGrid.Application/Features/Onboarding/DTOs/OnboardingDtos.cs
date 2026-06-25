using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Onboarding.DTOs;

public sealed record OnboardingPlanQuery(string? Status, Guid? EmployeeId, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}
public sealed record CreateOnboardingPlanRequest(Guid EmployeeId, Guid TemplateId, DateOnly StartDate, DateOnly? ProbationEndDate, Guid? ReviewerUserId);
public sealed record UpdateOnboardingPlanRequest(string Status);
public sealed record AddOnboardingTaskRequest(string ChecklistItem, string OwnerType, Guid? OwnerUserId, DateOnly DueDate, bool IsMandatory);
public sealed record ReopenTaskRequest(string Reason);
public sealed record AcknowledgePolicyRequest(Guid EmployeeId, Guid PolicyId);
public sealed record OnboardingDocumentForm(Guid? DocumentTypeId);
public sealed record OnboardingPlanDto(Guid Id, Guid EmployeeId, Guid TemplateId, DateOnly StartDate, string Status, DateTime? CompletedAt, int TotalTasks, int CompletedTasks);
public sealed record OnboardingTaskDto(Guid Id, string ChecklistItem, string OwnerType, Guid? OwnerUserId, DateOnly DueDate, string Status, bool IsMandatory);
public sealed record OnboardingProgressDto(Guid PlanId, int TotalTasks, int CompletedTasks, int MandatoryTasks, int CompletedMandatoryTasks, decimal PercentComplete);
