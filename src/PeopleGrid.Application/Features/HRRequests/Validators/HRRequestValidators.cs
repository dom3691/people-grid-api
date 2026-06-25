using FluentValidation;
using PeopleGrid.Application.Features.HRRequests.DTOs;

namespace PeopleGrid.Application.Features.HRRequests.Validators;

public sealed class CreateHRRequestRequestValidator : AbstractValidator<CreateHRRequestRequest>
{
    public CreateHRRequestRequestValidator()
    {
        RuleFor(x => x.RequestTypeId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(x => x is null || new[] { "Low", "Normal", "High", "Urgent" }.Contains(x)).WithMessage("Priority is invalid.");
    }
}

public sealed class UpdateHRRequestRequestValidator : AbstractValidator<UpdateHRRequestRequest>
{
    public UpdateHRRequestRequestValidator()
    {
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(x => x is null || new[] { "Low", "Normal", "High", "Urgent" }.Contains(x)).WithMessage("Priority is invalid.");
    }
}

public sealed class TransitionHRRequestRequestValidator : AbstractValidator<TransitionHRRequestRequest>
{
    public TransitionHRRequestRequestValidator()
    {
        RuleFor(x => x.Comments).MaximumLength(1000);
    }
}
