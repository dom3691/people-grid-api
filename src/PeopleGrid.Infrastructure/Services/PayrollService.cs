using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Payroll.DTOs;
using PeopleGrid.Application.Features.Payroll.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public sealed class PayrollService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IPayrollService
{
    public async Task<SalaryStructureDto> CreateSalaryStructureAsync(SalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (request.EmployeeId is null && request.GradeLevelId is null) throw new BusinessRuleException("Salary structure must target an employee or grade level.");
        var entity = new SalaryStructure { EmployeeId = request.EmployeeId, GradeLevelId = request.GradeLevelId, BasicSalary = request.BasicSalary, EffectiveDate = request.EffectiveDate, Status = request.Status };
        dbContext.SalaryStructures.Add(entity); AddAudit("CreateSalaryStructure", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<SalaryStructureDto>> ListSalaryStructuresAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.SalaryStructures.AsNoTracking().OrderByDescending(x => x.EffectiveDate).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<SalaryStructureDto> UpdateSalaryStructureAsync(Guid id, SalaryStructureRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await dbContext.SalaryStructures.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Salary structure was not found.");
        entity.EmployeeId = request.EmployeeId; entity.GradeLevelId = request.GradeLevelId; entity.BasicSalary = request.BasicSalary; entity.EffectiveDate = request.EffectiveDate; entity.Status = request.Status;
        AddAudit("UpdateSalaryStructure", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<PayrollItemDto> CreatePayrollItemAsync(PayrollItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (await dbContext.PayrollItems.AnyAsync(x => x.Code == request.Code.Trim().ToUpper(), cancellationToken)) throw new BusinessRuleException("Payroll item code already exists.");
        var entity = new PayrollItem { Code = request.Code.Trim().ToUpper(), Name = request.Name.Trim(), Type = request.Type, Taxable = request.Taxable, Pensionable = request.Pensionable, Recurring = request.Recurring, CalculationMethod = request.CalculationMethod, Amount = request.Amount, Percentage = request.Percentage, Status = request.Status };
        dbContext.PayrollItems.Add(entity); AddAudit("CreatePayrollItem", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<PayrollItemDto>> ListPayrollItemsAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.PayrollItems.AsNoTracking().OrderBy(x => x.Code).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<PayrollItemDto> UpdatePayrollItemAsync(Guid id, PayrollItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await dbContext.PayrollItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Payroll item was not found.");
        entity.Name = request.Name.Trim(); entity.Type = request.Type; entity.Taxable = request.Taxable; entity.Pensionable = request.Pensionable; entity.Recurring = request.Recurring; entity.CalculationMethod = request.CalculationMethod; entity.Amount = request.Amount; entity.Percentage = request.Percentage; entity.Status = request.Status;
        AddAudit("UpdatePayrollItem", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<EmployeeLoanDto> CreateLoanAsync(EmployeeLoanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!await dbContext.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken)) throw new BusinessRuleException("Employee is invalid.");
        var entity = new EmployeeLoan { EmployeeId = request.EmployeeId, LoanAmount = request.LoanAmount, InterestFee = request.InterestFee, RepaymentStartDate = request.RepaymentStartDate, MonthlyRepayment = request.MonthlyRepayment, OutstandingBalance = request.LoanAmount + request.InterestFee };
        dbContext.EmployeeLoans.Add(entity); AddAudit("CreateLoan", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<EmployeeLoanDto>> ListLoansAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.EmployeeLoans.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<PayrollRunDto> CreatePayrollRunAsync(CreatePayrollRunRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (await dbContext.PayrollRuns.AnyAsync(x => x.Period == request.Period, cancellationToken)) throw new BusinessRuleException("Payroll period already exists.");
        var entity = new PayrollRun { Period = request.Period.Trim(), Status = "Draft", PreparedBy = CurrentUserGuid() };
        dbContext.PayrollRuns.Add(entity); AddHistory(entity.Id, "Created", null); await dbContext.SaveChangesAsync(cancellationToken); return await MapRunAsync(entity.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PayrollRunDto>> ListPayrollRunsAsync(CancellationToken cancellationToken = default) { EnsureView(); var runs = await dbContext.PayrollRuns.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken); return await MapRunsAsync(runs, cancellationToken); }
    public async Task<PayrollRunDto> GetPayrollRunAsync(Guid id, CancellationToken cancellationToken = default) { EnsureView(); return await MapRunAsync(id, cancellationToken); }

    public async Task<PayrollRunDto> ProcessPayrollAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureProcess();
        var run = await dbContext.PayrollRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Payroll run was not found.");
        if (run.Status is "Finalized") throw new BusinessRuleException("Finalized payroll is immutable.");
        dbContext.PayrollRunEmployees.RemoveRange(dbContext.PayrollRunEmployees.Where(x => x.PayrollRunId == id));
        var employees = await dbContext.Employees.AsNoTracking().Where(x => x.Status == "Active").ToListAsync(cancellationToken);
        var items = await dbContext.EmployeePayrollItems.AsNoTracking().Include(x => x.PayrollItem).Where(x => x.Status == "Active").ToListAsync(cancellationToken);
        var salaryStructures = await dbContext.SalaryStructures.AsNoTracking().Where(x => x.Status == "Active").ToListAsync(cancellationToken);
        foreach (var employee in employees)
        {
            var basic = salaryStructures.Where(x => x.EmployeeId == employee.Id || (x.EmployeeId == null && x.GradeLevelId == employee.GradeLevelId)).OrderByDescending(x => x.EmployeeId == employee.Id).ThenByDescending(x => x.EffectiveDate).Select(x => x.BasicSalary).FirstOrDefault();
            var row = new PayrollRunEmployee { PayrollRunId = id, EmployeeId = employee.Id };
            var gross = basic; var deductions = 0m;
            foreach (var epi in items.Where(x => x.EmployeeId == employee.Id && x.PayrollItem is not null))
            {
                var amount = epi.Amount ?? (epi.Percentage is null ? epi.PayrollItem!.Amount ?? 0 : Math.Round(basic * epi.Percentage.Value / 100, 2));
                if (epi.PayrollItem!.Type.Equals("Deduction", StringComparison.OrdinalIgnoreCase)) { deductions += amount; row.Deductions.Add(new PayrollDeduction { ItemId = epi.PayrollItemId, Amount = amount, CalculationBasis = epi.PayrollItem.CalculationMethod }); }
                else { gross += amount; row.Earnings.Add(new PayrollEarning { ItemId = epi.PayrollItemId, Amount = amount, CalculationBasis = epi.PayrollItem.CalculationMethod }); }
            }
            var loanDeduction = await ApplyLoanRepaymentAsync(employee.Id, id, cancellationToken);
            deductions += loanDeduction;
            row.GrossSalary = gross; row.TotalDeductions = deductions; row.NetSalary = gross - deductions;
            if (row.NetSalary < 0) throw new BusinessRuleException("Net pay cannot be negative.");
            dbContext.PayrollRunEmployees.Add(row);
        }
        run.Status = "Processing"; AddHistory(id, "Processed", null); await dbContext.SaveChangesAsync(cancellationToken); return await MapRunAsync(id, cancellationToken);
    }

    public async Task<PayrollRunDto> ReviewPayrollAsync(Guid id, PayrollDecisionRequest request, CancellationToken cancellationToken = default) => await TransitionAsync(id, "Processing", "Reviewed", "Reviewed", 1, request.Comments, cancellationToken);
    public async Task<PayrollRunDto> ApprovePayrollAsync(Guid id, PayrollDecisionRequest request, CancellationToken cancellationToken = default) => await TransitionAsync(id, "Reviewed", "Approved", "Approved", 2, request.Comments, cancellationToken);

    public async Task<PayrollRunDto> FinalizePayrollAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureProcess(); var run = await dbContext.PayrollRuns.Include(x => x.Employees).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Payroll run was not found.");
        if (run.Status != "Approved") throw new BusinessRuleException("Finalization requires approved payroll.");
        run.Status = "Finalized"; run.FinalizedAt = DateTime.UtcNow;
        foreach (var row in run.Employees)
            if (!await dbContext.Payslips.AnyAsync(x => x.PayrollRunEmployeeId == row.Id, cancellationToken)) dbContext.Payslips.Add(new Payslip { PayrollRunEmployeeId = row.Id, PayslipNumber = $"PAY-{run.Period}-{row.EmployeeId.ToString()[..8]}".ToUpperInvariant(), GeneratedAt = DateTime.UtcNow });
        AddHistory(id, "Finalized", null); await dbContext.SaveChangesAsync(cancellationToken); return await MapRunAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PayslipDto>> GetPayslipsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await dbContext.Payslips.AsNoTracking().Include(x => x.PayrollRunEmployee).Where(x => x.PayrollRunEmployee != null && x.PayrollRunEmployee.EmployeeId == employeeId).Select(x => new PayslipDto(x.Id, x.PayrollRunEmployeeId, x.PayslipNumber, x.GeneratedAt, x.FileReference)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PayrollRunDto>> GetReportsAsync(string? period, CancellationToken cancellationToken = default)
    {
        EnsureView(); var source = dbContext.PayrollRuns.AsNoTracking().AsQueryable(); if (!string.IsNullOrWhiteSpace(period)) source = source.Where(x => x.Period == period); return await MapRunsAsync(await source.ToListAsync(cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetAuditHistoryAsync(Guid? payrollRunId, CancellationToken cancellationToken = default)
    {
        EnsureView(); var source = dbContext.PayrollAuditHistories.AsNoTracking().AsQueryable(); if (payrollRunId is not null) source = source.Where(x => x.PayrollRunId == payrollRunId); return await source.OrderByDescending(x => x.Timestamp).Select(x => $"{x.Timestamp:o} {x.Action} {x.Actor} {x.SnapshotData}").ToListAsync(cancellationToken);
    }

    private async Task<PayrollRunDto> TransitionAsync(Guid id, string requiredStatus, string nextStatus, string decision, int step, string? comments, CancellationToken ct)
    {
        EnsureProcess(); var run = await dbContext.PayrollRuns.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Payroll run was not found.");
        if (run.Status != requiredStatus) throw new BusinessRuleException($"Payroll must be {requiredStatus} before this action.");
        run.Status = nextStatus; if (nextStatus == "Reviewed") run.ReviewedBy = CurrentUserGuid(); if (nextStatus == "Approved") run.ApprovedBy = CurrentUserGuid();
        dbContext.PayrollApprovalActions.Add(new PayrollApprovalAction { PayrollRunId = id, Step = step, ActorUserId = CurrentUserGuid(), Decision = decision, Comments = comments });
        AddHistory(id, decision, comments); await dbContext.SaveChangesAsync(ct); return await MapRunAsync(id, ct);
    }

    private async Task<decimal> ApplyLoanRepaymentAsync(Guid employeeId, Guid runId, CancellationToken ct)
    {
        var loan = await dbContext.EmployeeLoans.FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LoanStatus == "Active" && x.OutstandingBalance > 0, ct);
        if (loan is null) return 0;
        var amount = Math.Min(loan.MonthlyRepayment, loan.OutstandingBalance);
        loan.OutstandingBalance -= amount; if (loan.OutstandingBalance <= 0) loan.LoanStatus = "Closed";
        dbContext.LoanRepayments.Add(new LoanRepayment { LoanId = loan.Id, PayrollRunId = runId, Amount = amount, RepaymentDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        return amount;
    }

    private async Task<IReadOnlyCollection<PayrollRunDto>> MapRunsAsync(IEnumerable<PayrollRun> runs, CancellationToken ct) { var list = new List<PayrollRunDto>(); foreach (var run in runs) list.Add(await MapRunAsync(run.Id, ct)); return list; }
    private async Task<PayrollRunDto> MapRunAsync(Guid id, CancellationToken ct)
    {
        var run = await dbContext.PayrollRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Payroll run was not found.");
        var rows = dbContext.PayrollRunEmployees.AsNoTracking().Where(x => x.PayrollRunId == id);
        return new PayrollRunDto(run.Id, run.Period, run.Status, await rows.CountAsync(ct), await rows.SumAsync(x => x.GrossSalary, ct), await rows.SumAsync(x => x.TotalDeductions, ct), await rows.SumAsync(x => x.NetSalary, ct));
    }

    private static SalaryStructureDto Map(SalaryStructure x) => new(x.Id, x.EmployeeId, x.GradeLevelId, x.BasicSalary, x.EffectiveDate, x.Status);
    private static PayrollItemDto Map(PayrollItem x) => new(x.Id, x.Code, x.Name, x.Type, x.Taxable, x.Pensionable, x.Recurring, x.CalculationMethod, x.Amount, x.Percentage, x.Status);
    private static EmployeeLoanDto Map(EmployeeLoan x) => new(x.Id, x.EmployeeId, x.LoanAmount, x.InterestFee, x.MonthlyRepayment, x.OutstandingBalance, x.LoanStatus);
    private void AddHistory(Guid runId, string action, string? snapshot) => dbContext.PayrollAuditHistories.Add(new PayrollAuditHistory { PayrollRunId = runId, Action = action, Actor = currentUser.UserId ?? "system", SnapshotData = snapshot });
    private void AddAudit(string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = "Payroll", Action = action, EntityType = "Payroll", EntityId = id.ToString(), Outcome = "Success" });
    private void EnsureView() { if (!currentUser.Permissions.Contains("Payroll.View") && !currentUser.Permissions.Contains("Payroll.Manage") && !currentUser.Permissions.Contains("Payroll.Process")) throw new ForbiddenException("Payroll view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Payroll.Manage")) throw new ForbiddenException("Payroll management permission is required."); }
    private void EnsureProcess() { if (!currentUser.Permissions.Contains("Payroll.Process") && !currentUser.Permissions.Contains("Payroll.Manage")) throw new ForbiddenException("Payroll process permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}

