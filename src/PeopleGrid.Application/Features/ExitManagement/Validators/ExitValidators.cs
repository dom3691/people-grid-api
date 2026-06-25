using FluentValidation;
using PeopleGrid.Application.Features.ExitManagement.DTOs;

namespace PeopleGrid.Application.Features.ExitManagement.Validators;

public sealed class SubmitResignationRequestValidator : AbstractValidator<SubmitResignationRequest>
{
    public SubmitResignationRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ResignationDate).NotEmpty();
        RuleFor(x => x.ProposedLastWorkingDay).GreaterThan(x => x.ResignationDate);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.NoticePeriod).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddExitClearanceItemRequestValidator : AbstractValidator<AddExitClearanceItemRequest>
{
    public AddExitClearanceItemRequestValidator() => RuleFor(x => x.ItemName).NotEmpty().MaximumLength(150);
}

