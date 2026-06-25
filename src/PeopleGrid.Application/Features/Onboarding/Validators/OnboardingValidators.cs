using FluentValidation;
using PeopleGrid.Application.Features.Onboarding.DTOs;

namespace PeopleGrid.Application.Features.Onboarding.Validators;

public sealed class CreateOnboardingPlanRequestValidator : AbstractValidator<CreateOnboardingPlanRequest>
{
    public CreateOnboardingPlanRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x).Must(x => x.ProbationEndDate is null || x.StartDate < x.ProbationEndDate).WithMessage("Probation end date must be after start date.");
    }
}

public sealed class AddOnboardingTaskRequestValidator : AbstractValidator<AddOnboardingTaskRequest>
{
    public AddOnboardingTaskRequestValidator()
    {
        RuleFor(x => x.ChecklistItem).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(50);
    }
}
