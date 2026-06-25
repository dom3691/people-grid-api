using FluentValidation;
using PeopleGrid.Application.Features.Payroll.DTOs;

namespace PeopleGrid.Application.Features.Payroll.Validators;

public sealed class SalaryStructureRequestValidator : AbstractValidator<SalaryStructureRequest>
{
    public SalaryStructureRequestValidator()
    {
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x).Must(x => x.EmployeeId is not null || x.GradeLevelId is not null).WithMessage("EmployeeId or GradeLevelId is required.");
    }
}

public sealed class PayrollItemRequestValidator : AbstractValidator<PayrollItemRequest>
{
    public PayrollItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).Must(x => x is "Earning" or "Deduction");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount is not null);
        RuleFor(x => x.Percentage).InclusiveBetween(0, 100).When(x => x.Percentage is not null);
    }
}

public sealed class CreatePayrollRunRequestValidator : AbstractValidator<CreatePayrollRunRequest>
{
    public CreatePayrollRunRequestValidator() => RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
}

