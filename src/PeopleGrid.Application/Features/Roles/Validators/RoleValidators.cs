using FluentValidation;
using PeopleGrid.Application.Features.Roles.DTOs;

namespace PeopleGrid.Application.Features.Roles.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    private static readonly string[] ValidStatuses = ["Active", "Inactive"];

    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("Role code must contain only uppercase letters, numbers, and underscores.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Status).NotEmpty().Must(x => ValidStatuses.Contains(x)).WithMessage("Status is invalid.");
        RuleFor(x => x.PermissionIds)
            .Must(x => x is null || x.Distinct().Count() == x.Count)
            .WithMessage("Duplicate permissions are not allowed.");
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    private static readonly string[] ValidStatuses = ["Active", "Inactive"];

    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("Role code must contain only uppercase letters, numbers, and underscores.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Status).NotEmpty().Must(x => ValidStatuses.Contains(x)).WithMessage("Status is invalid.");
    }
}

public sealed class AssignRolePermissionsRequestValidator : AbstractValidator<AssignRolePermissionsRequest>
{
    public AssignRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull();
        RuleFor(x => x.PermissionIds)
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Duplicate permissions are not allowed.");
    }
}
