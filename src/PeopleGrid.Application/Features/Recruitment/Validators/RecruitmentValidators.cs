using FluentValidation;
using PeopleGrid.Application.Features.Recruitment.DTOs;

namespace PeopleGrid.Application.Features.Recruitment.Validators;

public sealed class JobOpeningRequestValidator : AbstractValidator<JobOpeningRequest>
{
    public JobOpeningRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Vacancies).GreaterThan(0);
        RuleFor(x => x.EmploymentType).NotEmpty();
        RuleFor(x => x.ClosingDate).NotEmpty();
        RuleFor(x => x).Must(x => x.PublicationDate is null || x.ClosingDate >= x.PublicationDate).WithMessage("Closing date cannot be before publication date.");
    }
}

public sealed class ApplicationRequestValidator : AbstractValidator<ApplicationRequest>
{
    public ApplicationRequestValidator()
    {
        RuleFor(x => x.JobOpeningId).NotEmpty();
        RuleFor(x => x.CandidateName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

