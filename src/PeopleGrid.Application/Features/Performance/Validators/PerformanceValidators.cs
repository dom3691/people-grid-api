using FluentValidation;
using PeopleGrid.Application.Features.Performance.DTOs;

namespace PeopleGrid.Application.Features.Performance.Validators;

public sealed class PerformanceCycleRequestValidator : AbstractValidator<PerformanceCycleRequest>
{
    public PerformanceCycleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}

public sealed class EmployeeGoalRequestValidator : AbstractValidator<EmployeeGoalRequest>
{
    public EmployeeGoalRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.CycleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Target).NotEmpty();
        RuleFor(x => x.Weight).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

