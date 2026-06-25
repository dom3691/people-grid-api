using FluentValidation;
using PeopleGrid.Application.Features.Settings.DTOs;

namespace PeopleGrid.Application.Features.Settings.Validators;

public static class SettingsValidationRules
{
    public static readonly string[] ValidStatuses = ["Active", "Inactive"];

    public static IRuleBuilderOptions<T, string> ValidCode<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("Code must contain only uppercase letters, numbers, underscores, and hyphens.");
    }

    public static IRuleBuilderOptions<T, string> ValidStatus<T>(this IRuleBuilder<T, string> rule)
    {
        return rule.NotEmpty().Must(x => ValidStatuses.Contains(x)).WithMessage("Status is invalid.");
    }
}

public sealed class CompanyProfileRequestValidator : AbstractValidator<CompanyProfileRequest>
{
    public CompanyProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.LogoPath).MaximumLength(500);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}

public sealed class GradeLevelRequestValidator : AbstractValidator<GradeLevelRequest>
{
    public GradeLevelRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RankOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class EmploymentTypeRequestValidator : AbstractValidator<EmploymentTypeRequest>
{
    public EmploymentTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class ApprovalLevelRequestValidator : AbstractValidator<ApprovalLevelRequest>
{
    public ApprovalLevelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SequenceOrder).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class LeaveTypeRequestValidator : AbstractValidator<LeaveTypeRequest>
{
    public LeaveTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DefaultDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class PublicHolidayRequestValidator : AbstractValidator<PublicHolidayRequest>
{
    public PublicHolidayRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.HolidayDate).NotEmpty();
        RuleFor(x => x.LocationScope).MaximumLength(100);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class SystemParameterRequestValidator : AbstractValidator<SystemParameterRequest>
{
    public SystemParameterRequestValidator()
    {
        RuleFor(x => x.Value).NotNull().MaximumLength(2000);
    }
}
