using PeopleGrid.Application.Features.Payroll.DTOs;

namespace PeopleGrid.Application.Features.Payroll.Interfaces;

public interface IPayrollService
{
    Task<SalaryStructureDto> CreateSalaryStructureAsync(SalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SalaryStructureDto>> ListSalaryStructuresAsync(CancellationToken cancellationToken = default);
    Task<SalaryStructureDto> UpdateSalaryStructureAsync(Guid id, SalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<PayrollItemDto> CreatePayrollItemAsync(PayrollItemRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PayrollItemDto>> ListPayrollItemsAsync(CancellationToken cancellationToken = default);
    Task<PayrollItemDto> UpdatePayrollItemAsync(Guid id, PayrollItemRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeLoanDto> CreateLoanAsync(EmployeeLoanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeLoanDto>> ListLoansAsync(CancellationToken cancellationToken = default);
    Task<PayrollRunDto> CreatePayrollRunAsync(CreatePayrollRunRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PayrollRunDto>> ListPayrollRunsAsync(CancellationToken cancellationToken = default);
    Task<PayrollRunDto> GetPayrollRunAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> ProcessPayrollAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> ReviewPayrollAsync(Guid id, PayrollDecisionRequest request, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> ApprovePayrollAsync(Guid id, PayrollDecisionRequest request, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> FinalizePayrollAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PayslipDto>> GetPayslipsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PayrollRunDto>> GetReportsAsync(string? period, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetAuditHistoryAsync(Guid? payrollRunId, CancellationToken cancellationToken = default);
}

