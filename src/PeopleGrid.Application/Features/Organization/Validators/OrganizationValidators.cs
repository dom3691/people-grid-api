using FluentValidation;
using PeopleGrid.Application.Features.Organization.DTOs;

namespace PeopleGrid.Application.Features.Organization.Validators;

public static class OrganizationValidationRules
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

public sealed class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.StateRegion).MaximumLength(100);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.StateRegion).MaximumLength(100);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class CreateJobTitleRequestValidator : AbstractValidator<CreateJobTitleRequest>
{
    public CreateJobTitleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class UpdateJobTitleRequestValidator : AbstractValidator<UpdateJobTitleRequest>
{
    public UpdateJobTitleRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Status).ValidStatus();
    }
}

public sealed class AssignManagerRequestValidator : AbstractValidator<AssignManagerRequest>
{
    public AssignManagerRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ManagerUserId).NotEmpty();
        RuleFor(x => x.ManagerUserId).NotEqual(x => x.UserId).WithMessage("A user cannot be assigned as their own manager.");
        RuleFor(x => x.EffectiveFrom).NotEmpty();
    }
}
