using FluentValidation;
using PeopleGrid.Application.Features.Training.DTOs;

namespace PeopleGrid.Application.Features.Training.Validators;

public sealed class TrainingProgramRequestValidator : AbstractValidator<TrainingProgramRequest>
{
    public TrainingProgramRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
    }
}

public sealed class TrainingFeedbackRequestValidator : AbstractValidator<TrainingFeedbackRequest>
{
    public TrainingFeedbackRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(1, 5);
    }
}

