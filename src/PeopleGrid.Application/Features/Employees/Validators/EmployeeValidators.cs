using FluentValidation;
using PeopleGrid.Application.Features.Employees.DTOs;

namespace PeopleGrid.Application.Features.Employees.Validators;

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeNumber).MaximumLength(50);
        RuleFor(x => x.PersonalInfo).NotNull().SetValidator(new EmployeePersonalInfoRequestValidator());
        RuleFor(x => x.ContactInfo).NotNull().SetValidator(new EmployeeContactInfoRequestValidator());
        RuleFor(x => x.EmploymentInfo).NotNull().SetValidator(new EmployeeEmploymentInfoRequestValidator());
        RuleFor(x => x.Status).NotEmpty().Must(EmployeeValidation.ValidStatus).WithMessage("Status is invalid.");
        When(x => x.Status == "Active", () =>
        {
            RuleFor(x => x.EmploymentInfo.DepartmentId).NotEmpty();
            RuleFor(x => x.EmploymentInfo.JobTitleId).NotEmpty();
            RuleFor(x => x.EmploymentInfo.GradeLevelId).NotEmpty();
        });
    }
}

public sealed class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.PersonalInfo).NotNull().SetValidator(new EmployeePersonalInfoRequestValidator());
        RuleFor(x => x.ContactInfo).NotNull().SetValidator(new EmployeeContactInfoRequestValidator());
        RuleFor(x => x.EmploymentInfo).NotNull().SetValidator(new EmployeeEmploymentInfoRequestValidator());
        RuleFor(x => x.Status).NotEmpty().Must(EmployeeValidation.ValidStatus).WithMessage("Status is invalid.");
    }
}

public sealed class EmployeePersonalInfoRequestValidator : AbstractValidator<EmployeePersonalInfoRequest>
{
    public EmployeePersonalInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth)
            .Must(x => x <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)))
            .WithMessage("Employee must satisfy the minimum working-age policy.");
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MaritalStatus).MaximumLength(50);
        RuleFor(x => x.Nationality).MaximumLength(100);
    }
}

public sealed class EmployeeContactInfoRequestValidator : AbstractValidator<EmployeeContactInfoRequest>
{
    public EmployeeContactInfoRequestValidator()
    {
        RuleFor(x => x.WorkEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PersonalEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.Country).MaximumLength(100);
    }
}

public sealed class EmployeeEmploymentInfoRequestValidator : AbstractValidator<EmployeeEmploymentInfoRequest>
{
    public EmployeeEmploymentInfoRequestValidator()
    {
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x).Must(x => x.ConfirmationDate is null || x.HireDate <= x.ConfirmationDate)
            .WithMessage("Employment date cannot be after confirmation date.");
    }
}

public sealed class EmployeeBankInfoRequestValidator : AbstractValidator<EmployeeBankInfoRequest>
{
    public EmployeeBankInfoRequestValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BankCode).MaximumLength(50);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().Matches("^[0-9]{10,20}$").WithMessage("Bank account number format is invalid.");
    }
}

public sealed class EmployeeNextOfKinRequestValidator : AbstractValidator<EmployeeNextOfKinRequest>
{
    public EmployeeNextOfKinRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public sealed class EmployeeEmergencyContactRequestValidator : AbstractValidator<EmployeeEmergencyContactRequest>
{
    public EmployeeEmergencyContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Priority).GreaterThan(0);
    }
}

public sealed class ChangeEmployeeStatusRequestValidator : AbstractValidator<ChangeEmployeeStatusRequest>
{
    public ChangeEmployeeStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().Must(EmployeeValidation.ValidStatus).WithMessage("Status is invalid.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class DeactivateEmployeeRequestValidator : AbstractValidator<DeactivateEmployeeRequest>
{
    public DeactivateEmployeeRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

internal static class EmployeeValidation
{
    private static readonly string[] Statuses = ["Active", "Inactive", "Probation", "Confirmed", "Suspended", "Terminated", "Deactivated"];
    public static bool ValidStatus(string status) => Statuses.Contains(status);
}
