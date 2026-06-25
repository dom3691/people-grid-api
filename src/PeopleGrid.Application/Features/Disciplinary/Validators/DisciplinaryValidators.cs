using FluentValidation;
using PeopleGrid.Application.Features.Disciplinary.DTOs;

namespace PeopleGrid.Application.Features.Disciplinary.Validators;

public sealed class CreateDisciplinaryCaseRequestValidator : AbstractValidator<CreateDisciplinaryCaseRequest>
{
    public CreateDisciplinaryCaseRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.QueryDetails).NotEmpty().MaximumLength(4000);
        RuleFor(x => x).Must(x => x.IssueDate < x.ResponseDueDate).WithMessage("Response due date must be after issue date.");
    }
}

public sealed class SuspensionRequestValidator : AbstractValidator<SuspensionRequest>
{
    public SuspensionRequestValidator()
    {
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate).WithMessage("Suspension end date must be after or equal to start date.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
