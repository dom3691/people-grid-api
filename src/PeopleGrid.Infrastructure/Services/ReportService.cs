using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Reports.DTOs;
using PeopleGrid.Application.Features.Reports.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Infrastructure.Services;

public sealed class ReportService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IReportService
{
    private const int LargeExportThreshold = 10000;

    public async Task<IReadOnlyCollection<DashboardCardDto>> GetDashboardCardsAsync(CancellationToken cancellationToken = default)
    {
        EnsureDashboardView();
        var roleIds = await ResolveCurrentRoleIdsAsync(cancellationToken);
        var cards = await dbContext.DashboardCards.AsNoTracking()
            .Include(x => x.RoleMappings)
            .Where(x => x.IsActive && (!x.RoleMappings.Any() || x.RoleMappings.Any(m => roleIds.Contains(m.RoleId))))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (cards.Count == 0) return await DefaultDashboardCardsAsync(cancellationToken);

        var results = new List<DashboardCardDto>();
        foreach (var card in cards)
        {
            var scope = card.RoleMappings.FirstOrDefault(x => roleIds.Contains(x.RoleId))?.Scope ?? "enterprise";
            results.Add(new DashboardCardDto(card.CardCode, card.Title, card.Module, scope, await ResolveMetricAsync(card.CardCode, cancellationToken), card.DisplayOrder, ResolveDrillDownUrl(card.CardCode)));
        }
        return results;
    }

    public async Task<IReadOnlyCollection<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync(CancellationToken cancellationToken = default)
    {
        EnsureDashboardView();
        var rows = await dbContext.Departments.AsNoTracking()
            .GroupJoin(dbContext.Employees.AsNoTracking().Where(e => e.Status == "Active"), d => d.Id, e => e.DepartmentId, (d, e) => new { d.Id, d.Name, Count = e.Count() })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new DepartmentHeadcountDto(x.Id, x.Name, x.Count)).ToList();
    }

    public async Task<PendingApprovalsDto> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        EnsureDashboardView();
        var hr = await dbContext.HRRequests.CountAsync(x => x.Status == "Pending Approval", cancellationToken);
        var leave = await dbContext.LeaveRequests.CountAsync(x => x.Status == "Pending Approval" || x.Status == "Submitted", cancellationToken);
        var steps = await dbContext.ApprovalInstanceSteps.CountAsync(x => x.Status == "Pending", cancellationToken);
        return new PendingApprovalsDto(hr, leave, steps, hr + leave + steps);
    }

    public async Task<ReportResultDto> GetEmployeeReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.Employees.AsNoTracking().Include(x => x.PersonalInfo).Include(x => x.EmploymentInfo).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.Id == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.DepartmentId == query.DepartmentId || (x.EmploymentInfo != null && x.EmploymentInfo.DepartmentId == query.DepartmentId));
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.CreatedAt >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.CreatedAt <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderBy(x => x.EmployeeNumber).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("EMPLOYEE_REPORT", "view", query, cancellationToken);
        return Result("EMPLOYEE_REPORT", "Employee Report", rows.Select(x => Row(("EmployeeNumber", x.EmployeeNumber), ("Name", FullName(x.PersonalInfo?.FirstName, x.PersonalInfo?.LastName)), ("WorkEmail", x.WorkEmail), ("Status", x.Status), ("DepartmentId", x.DepartmentId))), page, total);
    }

    public async Task<ReportResultDto> GetLeaveReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.LeaveRequests.AsNoTracking().Include(x => x.LeaveTypeDefinition).Include(x => x.Employee).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee != null && x.Employee.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.LeaveType)) source = source.Where(x => x.LeaveTypeDefinition != null && x.LeaveTypeDefinition.Code == query.LeaveType);
        if (query.FromDate is not null) source = source.Where(x => x.StartDate >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.EndDate <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.StartDate).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("LEAVE_REPORT", "view", query, cancellationToken);
        return Result("LEAVE_REPORT", "Leave Report", rows.Select(x => Row(("EmployeeId", x.EmployeeId), ("LeaveType", x.LeaveTypeDefinition?.Name), ("StartDate", x.StartDate), ("EndDate", x.EndDate), ("Days", x.Days), ("Status", x.Status))), page, total);
    }

    public async Task<ReportResultDto> GetAttendanceReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.AttendanceRecords.AsNoTracking().Include(x => x.Employee).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee != null && x.Employee.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.AttendanceDate >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.AttendanceDate <= query.ToDate);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.AttendanceDate).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("ATTENDANCE_REPORT", "view", query, cancellationToken);
        return Result("ATTENDANCE_REPORT", "Attendance Report", rows.Select(x => Row(("EmployeeId", x.EmployeeId), ("Date", x.AttendanceDate), ("ClockIn", x.ClockInAt), ("ClockOut", x.ClockOutAt), ("LateMinutes", x.LateMinutes), ("OvertimeMinutes", x.OvertimeMinutes), ("Status", x.Status))), page, total);
    }

    public async Task<ReportResultDto> GetPayrollReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.PayrollRunEmployees.AsNoTracking().Include(x => x.PayrollRun).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (!string.IsNullOrWhiteSpace(query.PayrollPeriod)) source = source.Where(x => x.PayrollRun != null && x.PayrollRun.Period == query.PayrollPeriod);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.PayrollRun != null && x.PayrollRun.Status == query.Status);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("PAYROLL_REPORT", "view", query, cancellationToken);
        return Result("PAYROLL_REPORT", "Payroll Report", rows.Select(x => Row(("PayrollPeriod", x.PayrollRun?.Period), ("EmployeeId", x.EmployeeId), ("GrossSalary", x.GrossSalary), ("TotalDeductions", x.TotalDeductions), ("NetSalary", x.NetSalary), ("Status", x.PayrollRun?.Status))), page, total);
    }

    public async Task<ReportResultDto> GetRecruitmentReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.CandidateApplications.AsNoTracking().Include(x => x.Candidate).Include(x => x.JobOpening).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.CandidateStatus)) source = source.Where(x => x.Status == query.CandidateStatus);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.DepartmentId is not null) source = source.Where(x => x.JobOpening != null && x.JobOpening.DepartmentId == query.DepartmentId);
        if (query.FromDate is not null) source = source.Where(x => x.AppliedAt >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.AppliedAt <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.AppliedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("RECRUITMENT_REPORT", "view", query, cancellationToken);
        return Result("RECRUITMENT_REPORT", "Recruitment Report", rows.Select(x => Row(("Candidate", x.Candidate?.Name), ("Email", x.Candidate?.Email), ("JobOpening", x.JobOpening?.Title), ("Status", x.Status), ("AppliedAt", x.AppliedAt))), page, total);
    }

    public async Task<ReportResultDto> GetTrainingReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.TrainingNominations.AsNoTracking().Include(x => x.Program).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.TrainingProgramId is not null) source = source.Where(x => x.ProgramId == query.TrainingProgramId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.NominatedAt >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.NominatedAt <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.NominatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("TRAINING_REPORT", "view", query, cancellationToken);
        return Result("TRAINING_REPORT", "Training Report", rows.Select(x => Row(("Program", x.Program?.Title), ("EmployeeId", x.EmployeeId), ("Status", x.Status), ("NominatedAt", x.NominatedAt))), page, total);
    }

    public async Task<ReportResultDto> GetExitReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.ExitCases.AsNoTracking().Include(x => x.Employee).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee != null && x.Employee.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.ResignationDate >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.ResignationDate <= query.ToDate);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.ResignationDate).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("EXIT_REPORT", "view", query, cancellationToken);
        return Result("EXIT_REPORT", "Exit Report", rows.Select(x => Row(("CaseNumber", x.CaseNumber), ("EmployeeId", x.EmployeeId), ("ResignationDate", x.ResignationDate), ("LastWorkingDay", x.LastWorkingDay), ("Status", x.Status))), page, total);
    }

    public async Task<ReportResultDto> GetDepartmentReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.Departments.AsNoTracking().AsQueryable();
        if (query.DepartmentId is not null) source = source.Where(x => x.Id == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderBy(x => x.Name).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("DEPARTMENT_REPORT", "view", query, cancellationToken);
        return Result("DEPARTMENT_REPORT", "Department Report", rows.Select(x => Row(("Code", x.Code), ("Name", x.Name), ("Status", x.Status), ("HeadUserId", x.HeadUserId))), page, total);
    }

    public async Task<ReportResultDto> GetAuditReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Outcome == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.Timestamp >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.Timestamp <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.Timestamp).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("AUDIT_REPORT", "view", query, cancellationToken);
        return Result("AUDIT_REPORT", "Audit Report", rows.Select(x => Row(("Timestamp", x.Timestamp), ("ActorUserId", x.ActorUserId), ("Module", x.Module), ("Action", x.Action), ("Outcome", x.Outcome), ("EntityType", x.EntityType), ("EntityId", x.EntityId))), page, total);
    }

    public async Task<ReportResultDto> GetPerformanceReportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query); EnsureReportView();
        var source = dbContext.PerformanceRatings.AsNoTracking().Include(x => x.Cycle).AsQueryable();
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (!string.IsNullOrWhiteSpace(query.Rating)) source = source.Where(x => x.FinalRating == query.Rating);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.IsReleased == (query.Status == "Released"));
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.ReleasedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        await AuditAsync("PERFORMANCE_REPORT", "view", query, cancellationToken);
        return Result("PERFORMANCE_REPORT", "Performance Report", rows.Select(x => Row(("EmployeeId", x.EmployeeId), ("Cycle", x.Cycle?.Name), ("FinalRating", x.FinalRating), ("IsReleased", x.IsReleased), ("ReleasedAt", x.ReleasedAt))), page, total);
    }

    public async Task<ReportExportJobDto> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuery(request.Filters); EnsureReportExport();
        if (request.Format is not ("PDF" or "Excel")) throw new BusinessRuleException("Export format must be PDF or Excel.");
        var report = await EnsureReportDefinitionAsync(request.ReportCode, cancellationToken);
        var preview = await GetReportByCodeAsync(request.ReportCode, request.Filters with { PageNumber = 1, PageSize = 1 }, cancellationToken);
        if (preview.TotalCount > LargeExportThreshold && !request.ConfirmLargeExport) throw new BusinessRuleException("Large exports require row-limit confirmation.");

        var job = new ReportExportJob { ReportId = report.Id, UserId = CurrentUserGuid(), FiltersJson = JsonSerializer.Serialize(request.Filters), Format = request.Format, Status = preview.TotalCount > 1000 ? "Queued" : "Completed", RequestedAt = DateTime.UtcNow, CompletedAt = preview.TotalCount > 1000 ? null : DateTime.UtcNow };
        dbContext.ReportExportJobs.Add(job);
        if (job.Status == "Completed")
        {
            var ext = request.Format == "PDF" ? "pdf" : "xlsx";
            dbContext.ReportExportFiles.Add(new ReportExportFile { ExportJobId = job.Id, FileName = $"{request.ReportCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}", StorageKey = $"reports/{job.Id}.{ext}", FileSize = 0 });
        }
        dbContext.ReportAuditLogs.Add(new ReportAuditLog { ReportId = report.Id, UserId = CurrentUserGuid(), Action = "export", FiltersJson = job.FiltersJson });
        await dbContext.SaveChangesAsync(cancellationToken);
        var file = await dbContext.ReportExportFiles.AsNoTracking().FirstOrDefaultAsync(x => x.ExportJobId == job.Id, cancellationToken);
        return new ReportExportJobDto(job.Id, report.ReportCode, job.Format, job.Status, job.RequestedAt, job.CompletedAt, file?.FileName, file?.StorageKey);
    }

    private async Task<ReportResultDto> GetReportByCodeAsync(string reportCode, ReportQuery query, CancellationToken ct) => reportCode.ToUpperInvariant() switch
    {
        "EMPLOYEE_REPORT" => await GetEmployeeReportAsync(query, ct),
        "LEAVE_REPORT" => await GetLeaveReportAsync(query, ct),
        "ATTENDANCE_REPORT" => await GetAttendanceReportAsync(query, ct),
        "PAYROLL_REPORT" => await GetPayrollReportAsync(query, ct),
        "RECRUITMENT_REPORT" => await GetRecruitmentReportAsync(query, ct),
        "TRAINING_REPORT" => await GetTrainingReportAsync(query, ct),
        "EXIT_REPORT" => await GetExitReportAsync(query, ct),
        "DEPARTMENT_REPORT" => await GetDepartmentReportAsync(query, ct),
        "AUDIT_REPORT" => await GetAuditReportAsync(query, ct),
        "PERFORMANCE_REPORT" => await GetPerformanceReportAsync(query, ct),
        _ => throw new NotFoundException("Report definition was not found.")
    };

    private async Task<IReadOnlyCollection<DashboardCardDto>> DefaultDashboardCardsAsync(CancellationToken ct)
    {
        var pending = await GetPendingApprovalsAsync(ct);
        return
        [
            new("TOTAL_EMPLOYEES", "Total employees", "Employees", "enterprise", await dbContext.Employees.CountAsync(ct), 1, "/employees"),
            new("ACTIVE_EMPLOYEES", "Active employees", "Employees", "enterprise", await dbContext.Employees.CountAsync(x => x.Status == "Active", ct), 2, "/reports/employees?status=Active"),
            new("EMPLOYEES_ON_LEAVE", "Employees on leave", "Leave", "enterprise", await dbContext.LeaveRequests.CountAsync(x => x.Status == "Approved" && x.StartDate <= DateTime.UtcNow && x.EndDate >= DateTime.UtcNow, ct), 3, "/reports/leave?status=Approved"),
            new("NEW_EMPLOYEES_THIS_MONTH", "New employees this month", "Employees", "enterprise", await dbContext.Employees.CountAsync(x => x.CreatedAt.Month == DateTime.UtcNow.Month && x.CreatedAt.Year == DateTime.UtcNow.Year, ct), 4, "/reports/employees"),
            new("PENDING_APPROVALS", "Pending approvals", "Approvals", "enterprise", pending.Total, 5, "/approvals/pending"),
            new("PAYROLL_SUMMARY", "Payroll summary", "Payroll", "enterprise", await dbContext.PayrollRunEmployees.SumAsync(x => x.NetSalary, ct), 6, "/reports/payroll")
        ];
    }

    private async Task<decimal> ResolveMetricAsync(string code, CancellationToken ct) => code.ToUpperInvariant() switch
    {
        "TOTAL_EMPLOYEES" => await dbContext.Employees.CountAsync(ct),
        "ACTIVE_EMPLOYEES" => await dbContext.Employees.CountAsync(x => x.Status == "Active", ct),
        "EMPLOYEES_ON_LEAVE" => await dbContext.LeaveRequests.CountAsync(x => x.Status == "Approved" && x.StartDate <= DateTime.UtcNow && x.EndDate >= DateTime.UtcNow, ct),
        "NEW_EMPLOYEES_THIS_MONTH" => await dbContext.Employees.CountAsync(x => x.CreatedAt.Month == DateTime.UtcNow.Month && x.CreatedAt.Year == DateTime.UtcNow.Year, ct),
        "PENDING_LEAVE_REQUESTS" => await dbContext.LeaveRequests.CountAsync(x => x.Status == "Submitted" || x.Status == "Pending Approval", ct),
        "PENDING_HR_REQUESTS" => await dbContext.HRRequests.CountAsync(x => x.Status == "Pending Approval", ct),
        "PENDING_APPROVALS" => (await GetPendingApprovalsAsync(ct)).Total,
        "PAYROLL_SUMMARY" => await dbContext.PayrollRunEmployees.SumAsync(x => x.NetSalary, ct),
        _ => await dbContext.DashboardMetricSnapshots.Where(x => x.Card != null && x.Card.CardCode == code).OrderByDescending(x => x.SnapshotAt).Select(x => x.Value).FirstOrDefaultAsync(ct)
    };

    private static string? ResolveDrillDownUrl(string code) => code.ToUpperInvariant() switch
    {
        "TOTAL_EMPLOYEES" => "/reports/employees",
        "ACTIVE_EMPLOYEES" => "/reports/employees?status=Active",
        "EMPLOYEES_ON_LEAVE" => "/reports/leave?status=Approved",
        "PENDING_LEAVE_REQUESTS" => "/reports/leave?status=Pending Approval",
        "PENDING_HR_REQUESTS" => "/hr-requests?status=Pending Approval",
        "PENDING_APPROVALS" => "/approvals/pending",
        "PAYROLL_SUMMARY" => "/reports/payroll",
        _ => null
    };

    private async Task<ReportDefinition> EnsureReportDefinitionAsync(string reportCode, CancellationToken ct)
    {
        var normalized = reportCode.Trim().ToUpperInvariant();
        var report = await dbContext.ReportDefinitions.FirstOrDefaultAsync(x => x.ReportCode == normalized, ct);
        if (report is not null) return report;
        report = new ReportDefinition { ReportCode = normalized, Name = normalized.Replace('_', ' '), Module = normalized.Split('_')[0], Permissions = "Report.View" };
        dbContext.ReportDefinitions.Add(report);
        await dbContext.SaveChangesAsync(ct);
        return report;
    }

    private async Task AuditAsync(string reportCode, string action, ReportQuery query, CancellationToken ct)
    {
        var report = await EnsureReportDefinitionAsync(reportCode, ct);
        dbContext.ReportAuditLogs.Add(new ReportAuditLog { ReportId = report.Id, UserId = CurrentUserGuid(), Action = action, FiltersJson = JsonSerializer.Serialize(query) });
        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveCurrentRoleIdsAsync(CancellationToken ct)
    {
        var userRoles = currentUser.Roles.Select(x => x.Replace(" ", string.Empty).ToUpperInvariant()).ToArray();
        return await dbContext.Roles.AsNoTracking().Where(x => currentUser.Roles.Contains(x.Name) || userRoles.Contains(x.Code)).Select(x => x.Id).ToListAsync(ct);
    }

    private static ReportResultDto Result(string code, string name, IEnumerable<ReportRowDto> rows, PaginationRequest page, int total) => new(code, name, rows.ToList(), page.PageNumber, page.Take, total);
    private static ReportRowDto Row(params (string Key, object? Value)[] values) => new(values.ToDictionary(x => x.Key, x => x.Value));
    private static string FullName(string? firstName, string? lastName) => string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static void ValidateQuery(ReportQuery query) { if (query.FromDate is not null && query.ToDate is not null && query.FromDate > query.ToDate) throw new BusinessRuleException("Date range start must be before or equal to end date."); }
    private void EnsureDashboardView() { if (!currentUser.Permissions.Contains("Dashboard.View") && !currentUser.Permissions.Contains("Report.View") && !currentUser.Permissions.Contains("Report.Export")) throw new ForbiddenException("Dashboard view permission is required."); }
    private void EnsureReportView() { if (!currentUser.Permissions.Contains("Report.View") && !currentUser.Permissions.Contains("Report.Export")) throw new ForbiddenException("Report view permission is required."); }
    private void EnsureReportExport() { if (!currentUser.Permissions.Contains("Report.Export")) throw new ForbiddenException("Report export permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}
