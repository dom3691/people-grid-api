using FluentValidation;
using PeopleGrid.Application.Features.Leave.DTOs;

namespace PeopleGrid.Application.Features.Leave.Validators;

public sealed class CreateLeaveRequestRequestValidator : AbstractValidator<CreateLeaveRequestRequest>
{
    public CreateLeaveRequestRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate).WithMessage("Start date must be before or equal to end date.");
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class LeaveDecisionRequestValidator : AbstractValidator<LeaveDecisionRequest>
{
    public LeaveDecisionRequestValidator()
    {
        RuleFor(x => x.Comments).MaximumLength(1000);
    }
}

public sealed class LeaveEntitlementRequestValidator : AbstractValidator<LeaveEntitlementRequest>
{
    public LeaveEntitlementRequestValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.EntitlementDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
    }
}
