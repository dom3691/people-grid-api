using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Payroll.DTOs;
using PeopleGrid.Application.Features.Payroll.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public sealed class PayrollController(IPayrollService payrollService) : ControllerBase
{
    [HttpPost("salary-structures")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<SalaryStructureDto>>> CreateSalary(SalaryStructureRequest request, CancellationToken ct) => Ok(ApiResponse<SalaryStructureDto>.Ok(await payrollService.CreateSalaryStructureAsync(request, ct)));
    [HttpGet("salary-structures")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SalaryStructureDto>>>> SalaryStructures(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<SalaryStructureDto>>.Ok(await payrollService.ListSalaryStructuresAsync(ct)));
    [HttpPut("salary-structures/{id:guid}")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<SalaryStructureDto>>> UpdateSalary(Guid id, SalaryStructureRequest request, CancellationToken ct) => Ok(ApiResponse<SalaryStructureDto>.Ok(await payrollService.UpdateSalaryStructureAsync(id, request, ct)));
    [HttpPost("items")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<PayrollItemDto>>> CreateItem(PayrollItemRequest request, CancellationToken ct) => Ok(ApiResponse<PayrollItemDto>.Ok(await payrollService.CreatePayrollItemAsync(request, ct)));
    [HttpGet("items")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PayrollItemDto>>>> Items(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PayrollItemDto>>.Ok(await payrollService.ListPayrollItemsAsync(ct)));
    [HttpPut("items/{id:guid}")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<PayrollItemDto>>> UpdateItem(Guid id, PayrollItemRequest request, CancellationToken ct) => Ok(ApiResponse<PayrollItemDto>.Ok(await payrollService.UpdatePayrollItemAsync(id, request, ct)));
    [HttpPost("loans")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<EmployeeLoanDto>>> CreateLoan(EmployeeLoanRequest request, CancellationToken ct) => Ok(ApiResponse<EmployeeLoanDto>.Ok(await payrollService.CreateLoanAsync(request, ct)));
    [HttpGet("loans")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeLoanDto>>>> Loans(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<EmployeeLoanDto>>.Ok(await payrollService.ListLoansAsync(ct)));
    [HttpPost("runs")][HasPermission("Payroll.Manage")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> CreateRun(CreatePayrollRunRequest request, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.CreatePayrollRunAsync(request, ct)));
    [HttpGet("runs")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PayrollRunDto>>>> Runs(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PayrollRunDto>>.Ok(await payrollService.ListPayrollRunsAsync(ct)));
    [HttpGet("runs/{id:guid}")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> Run(Guid id, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.GetPayrollRunAsync(id, ct)));
    [HttpPost("runs/{id:guid}/process")][HasPermission("Payroll.Process")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> Process(Guid id, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.ProcessPayrollAsync(id, ct)));
    [HttpPost("runs/{id:guid}/review")][HasPermission("Payroll.Process")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> Review(Guid id, PayrollDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.ReviewPayrollAsync(id, request, ct)));
    [HttpPost("runs/{id:guid}/approve")][HasPermission("Payroll.Process")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> Approve(Guid id, PayrollDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.ApprovePayrollAsync(id, request, ct)));
    [HttpPost("runs/{id:guid}/finalize")][HasPermission("Payroll.Process")] public async Task<ActionResult<ApiResponse<PayrollRunDto>>> Finalize(Guid id, CancellationToken ct) => Ok(ApiResponse<PayrollRunDto>.Ok(await payrollService.FinalizePayrollAsync(id, ct)));
    [HttpGet("payslips/{employeeId:guid}")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PayslipDto>>>> Payslips(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PayslipDto>>.Ok(await payrollService.GetPayslipsAsync(employeeId, ct)));
    [HttpGet("reports")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PayrollRunDto>>>> Reports([FromQuery] string? period, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PayrollRunDto>>.Ok(await payrollService.GetReportsAsync(period, ct)));
    [HttpGet("audit-history")][HasPermission("Payroll.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<string>>>> Audit([FromQuery] Guid? payrollRunId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<string>>.Ok(await payrollService.GetAuditHistoryAsync(payrollRunId, ct)));
}

