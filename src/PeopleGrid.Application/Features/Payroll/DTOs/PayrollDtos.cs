namespace PeopleGrid.Application.Features.Payroll.DTOs;

public sealed record SalaryStructureRequest(Guid? EmployeeId, Guid? GradeLevelId, decimal BasicSalary, DateOnly EffectiveDate, string Status = "Active");
public sealed record SalaryStructureDto(Guid Id, Guid? EmployeeId, Guid? GradeLevelId, decimal BasicSalary, DateOnly EffectiveDate, string Status);
public sealed record PayrollItemRequest(string Code, string Name, string Type, bool Taxable, bool Pensionable, bool Recurring, string CalculationMethod, decimal? Amount, decimal? Percentage, string Status = "Active");
public sealed record PayrollItemDto(Guid Id, string Code, string Name, string Type, bool Taxable, bool Pensionable, bool Recurring, string CalculationMethod, decimal? Amount, decimal? Percentage, string Status);
public sealed record EmployeeLoanRequest(Guid EmployeeId, decimal LoanAmount, decimal InterestFee, DateOnly RepaymentStartDate, decimal MonthlyRepayment);
public sealed record EmployeeLoanDto(Guid Id, Guid EmployeeId, decimal LoanAmount, decimal InterestFee, decimal MonthlyRepayment, decimal OutstandingBalance, string LoanStatus);
public sealed record CreatePayrollRunRequest(string Period);
public sealed record PayrollDecisionRequest(string? Comments);
public sealed record PayrollRunDto(Guid Id, string Period, string Status, int EmployeeCount, decimal GrossSalary, decimal TotalDeductions, decimal NetSalary);
public sealed record PayslipDto(Guid Id, Guid PayrollRunEmployeeId, string PayslipNumber, DateTime GeneratedAt, string? FileReference);

