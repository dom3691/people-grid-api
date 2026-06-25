using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Leave.DTOs;
using PeopleGrid.Application.Features.Leave.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Pagination;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class LeaveService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : ILeaveService
{
    public async Task<LeaveRequestDto> CreateRequestAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.Include(x => x.EmploymentInfo).FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee was not found.");
        var leaveType = await dbContext.LeaveTypes.FirstOrDefaultAsync(x => x.Id == request.LeaveTypeId && x.IsActive, cancellationToken) ?? throw new BusinessRuleException("Leave type is invalid.");
        var dates = await CalculateDatesAsync(request.StartDate, request.EndDate, leaveType, employee, cancellationToken);
        var leave = new LeaveRequest { EmployeeId = employee.Id, LeaveTypeId = leaveType.Id, LeaveType = leaveType.Code, StartDate = request.StartDate.ToDateTime(TimeOnly.MinValue), EndDate = request.EndDate.ToDateTime(TimeOnly.MinValue), Days = dates.Count(x => !x.Excluded && !x.IsHalfDay) + dates.Count(x => !x.Excluded && x.IsHalfDay) * 0.5m, Reason = request.Reason?.Trim(), Status = "Draft" };
        dbContext.LeaveRequests.Add(leave);
        foreach (var date in dates) { date.LeaveRequestId = leave.Id; dbContext.LeaveRequestDates.Add(date); }
        AddAudit("Leave", "CreateRequest", leave.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequestAsync(leave.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<LeaveRequestDto>> ListRequestsAsync(LeaveRequestListQuery query, CancellationToken cancellationToken = default)
    {
        var source = LoadRequests();
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.LeaveTypeId is not null) source = source.Where(x => x.LeaveTypeId == query.LeaveTypeId);
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee!.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (query.FromDate is not null) source = source.Where(x => x.StartDate >= query.FromDate.Value.ToDateTime(TimeOnly.MinValue));
        if (query.ToDate is not null) source = source.Where(x => x.EndDate <= query.ToDate.Value.ToDateTime(TimeOnly.MaxValue));
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var items = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<LeaveRequestDto>(items.Select(MapRequest).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<LeaveRequestDto> GetRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await LoadRequests().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave request was not found.");
        return MapRequest(leave);
    }

    public async Task<LeaveRequestDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leave = await dbContext.LeaveRequests.Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo).Include(x => x.LeaveTypeDefinition).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave request was not found.");
        if (leave.Status != "Draft") throw new BusinessRuleException("Only draft leave requests can be submitted.");
        await EnsureNoOverlapAsync(leave, cancellationToken);
        await EnsureSufficientBalanceAsync(leave, cancellationToken);
        leave.Status = "Pending Approval";
        leave.CurrentApproverId = leave.Employee?.EmploymentInfo?.LineManagerId;
        AddAudit("Leave", "Submit", leave.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequestAsync(id, cancellationToken);
    }

    public Task<LeaveRequestDto> ApproveAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default) => DecideAsync(id, "Approved", request.Comments, cancellationToken);
    public Task<LeaveRequestDto> RejectAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default) => DecideAsync(id, "Rejected", request.Comments, cancellationToken);

    public async Task<LeaveRequestDto> CancelAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var leave = await dbContext.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave request was not found.");
        if (leave.Status is "Approved" or "Rejected") throw new BusinessRuleException("Approved or rejected leave cannot be cancelled.");
        leave.Status = "Cancelled";
        AddAction(leave.Id, "Cancelled", request.Comments);
        AddAudit("Leave", "Cancel", leave.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequestAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.LeaveBalances.AsNoTracking().Include(x => x.LeaveType).Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.Year).Select(x => MapBalance(x)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LeaveHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.LeaveRequests.AsNoTracking().Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.StartDate).Select(x => new LeaveHistoryDto(x.Id, x.LeaveTypeId ?? Guid.Empty, x.LeaveType, x.StartDate, x.EndDate, x.Days, x.Status)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LeaveCalendarItemDto>> GetCalendarAsync(Guid? departmentId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var source = dbContext.LeaveRequestDates.AsNoTracking().Include(x => x.LeaveRequest!).ThenInclude(x => x!.Employee).ThenInclude(x => x!.PersonalInfo).Where(x => !x.Excluded && x.LeaveRequest!.Status == "Approved");
        if (departmentId is not null) source = source.Where(x => x.LeaveRequest!.Employee!.EmploymentInfo != null && x.LeaveRequest.Employee.EmploymentInfo.DepartmentId == departmentId);
        if (fromDate is not null) source = source.Where(x => x.Date >= fromDate);
        if (toDate is not null) source = source.Where(x => x.Date <= toDate);
        var rows = await source.OrderBy(x => x.Date).ToListAsync(cancellationToken);
        return rows.Select(x => new LeaveCalendarItemDto(x.LeaveRequestId, x.LeaveRequest!.EmployeeId, x.LeaveRequest.Employee?.PersonalInfo is null ? null : $"{x.LeaveRequest.Employee.PersonalInfo.FirstName} {x.LeaveRequest.Employee.PersonalInfo.LastName}".Trim(), x.Date, x.LeaveRequest.Status, x.LeaveRequest.LeaveType)).ToList();
    }

    public async Task<LeaveEntitlementDto> CreateEntitlementAsync(LeaveEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateEntitlementReferencesAsync(request, cancellationToken);
        var entity = new LeaveEntitlement { LeaveTypeId = request.LeaveTypeId, PolicyGroup = request.PolicyGroup?.Trim(), EmploymentTypeId = request.EmploymentTypeId, GradeLevelId = request.GradeLevelId, EntitlementDays = request.EntitlementDays, AccrualRule = request.AccrualRule?.Trim(), Year = request.Year };
        dbContext.LeaveEntitlements.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapEntitlementAsync(entity.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<LeaveEntitlementDto>> ListEntitlementsAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var page = new PaginationRequest(pageNumber, pageSize);
        var total = await dbContext.LeaveEntitlements.CountAsync(cancellationToken);
        var rows = await dbContext.LeaveEntitlements.AsNoTracking().Include(x => x.LeaveType).OrderByDescending(x => x.Year).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<LeaveEntitlementDto>(rows.Select(MapEntitlement).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<LeaveEntitlementDto> UpdateEntitlementAsync(Guid id, LeaveEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateEntitlementReferencesAsync(request, cancellationToken);
        var entity = await dbContext.LeaveEntitlements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave entitlement was not found.");
        entity.LeaveTypeId = request.LeaveTypeId; entity.PolicyGroup = request.PolicyGroup?.Trim(); entity.EmploymentTypeId = request.EmploymentTypeId; entity.GradeLevelId = request.GradeLevelId; entity.EntitlementDays = request.EntitlementDays; entity.AccrualRule = request.AccrualRule?.Trim(); entity.Year = request.Year;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapEntitlementAsync(id, cancellationToken);
    }

    private async Task<LeaveRequestDto> DecideAsync(Guid id, string decision, string? comments, CancellationToken cancellationToken)
    {
        var leave = await dbContext.LeaveRequests.Include(x => x.LeaveTypeDefinition).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave request was not found.");
        if (leave.Status != "Pending Approval") throw new BusinessRuleException("Leave request is not pending approval.");
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        leave.Status = decision;
        if (decision == "Approved") await ApplyBalanceUsageAsync(leave, cancellationToken);
        AddAction(leave.Id, decision, comments);
        AddAudit("Leave", decision, leave.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return await GetRequestAsync(id, cancellationToken);
    }

    private async Task<List<LeaveRequestDate>> CalculateDatesAsync(DateOnly start, DateOnly end, LeaveType type, Employee employee, CancellationToken ct)
    {
        var holidays = await dbContext.PublicHolidays.AsNoTracking().Where(x => x.IsActive && x.HolidayDate >= start && x.HolidayDate <= end && (x.BranchId == null || x.BranchId == employee.BranchId)).Select(x => x.HolidayDate).ToListAsync(ct);
        var dates = new List<LeaveRequestDate>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var holiday = holidays.Contains(date);
            dates.Add(new LeaveRequestDate { Date = date, Excluded = (type.ExcludesWeekends && weekend) || (type.ExcludesPublicHolidays && holiday), ExclusionReason = weekend ? "Weekend" : holiday ? "PublicHoliday" : null });
        }
        return dates;
    }

    private async Task EnsureNoOverlapAsync(LeaveRequest leave, CancellationToken ct)
    {
        var start = DateOnly.FromDateTime(leave.StartDate);
        var end = DateOnly.FromDateTime(leave.EndDate);
        var overlap = await dbContext.LeaveRequestDates.AnyAsync(x => x.LeaveRequestId != leave.Id && x.LeaveRequest!.EmployeeId == leave.EmployeeId && !x.Excluded && x.Date >= start && x.Date <= end && x.LeaveRequest.Status != "Rejected" && x.LeaveRequest.Status != "Cancelled", ct);
        if (overlap) throw new BusinessRuleException("Overlapping leave request exists for this employee.");
    }

    private async Task EnsureSufficientBalanceAsync(LeaveRequest leave, CancellationToken ct)
    {
        var balance = await dbContext.LeaveBalances.FirstOrDefaultAsync(x => x.EmployeeId == leave.EmployeeId && x.LeaveTypeId == leave.LeaveTypeId && x.Year == leave.StartDate.Year, ct);
        if (balance is not null && balance.Remaining < leave.Days) throw new BusinessRuleException("Leave balance is insufficient.");
    }

    private async Task ApplyBalanceUsageAsync(LeaveRequest leave, CancellationToken ct)
    {
        if (leave.LeaveTypeId is null) return;
        var balance = await dbContext.LeaveBalances.FirstOrDefaultAsync(x => x.EmployeeId == leave.EmployeeId && x.LeaveTypeId == leave.LeaveTypeId && x.Year == leave.StartDate.Year, ct);
        if (balance is null) return;
        balance.Used += leave.Days;
        balance.Remaining = balance.OpeningBalance + balance.Accrued - balance.Used + balance.Adjusted;
    }

    private IQueryable<LeaveRequest> LoadRequests() => dbContext.LeaveRequests.Include(x => x.Employee).ThenInclude(x => x!.PersonalInfo).Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo).Include(x => x.LeaveTypeDefinition);
    private void AddAction(Guid leaveRequestId, string decision, string? comments) => dbContext.LeaveApprovalActions.Add(new LeaveApprovalAction { LeaveRequestId = leaveRequestId, Step = 1, ActorUserId = CurrentUserGuid(), Decision = decision, Comments = comments?.Trim(), DecidedAt = DateTime.UtcNow });
    private void AddAudit(string module, string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "LeaveRequest", EntityId = id.ToString(), Outcome = "Success" });
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct) => dbContext is DbContext ef && ef.Database.CurrentTransaction is null ? await ef.Database.BeginTransactionAsync(ct) : null;
    private async Task ValidateEntitlementReferencesAsync(LeaveEntitlementRequest request, CancellationToken ct) { if (!await dbContext.LeaveTypes.AnyAsync(x => x.Id == request.LeaveTypeId && x.IsActive, ct)) throw new BusinessRuleException("Leave type is invalid."); }
    private async Task<LeaveEntitlementDto> MapEntitlementAsync(Guid id, CancellationToken ct) => MapEntitlement(await dbContext.LeaveEntitlements.AsNoTracking().Include(x => x.LeaveType).FirstAsync(x => x.Id == id, ct));
    private static LeaveRequestDto MapRequest(LeaveRequest x) => new(x.Id, x.EmployeeId, x.Employee?.PersonalInfo is null ? null : $"{x.Employee.PersonalInfo.FirstName} {x.Employee.PersonalInfo.LastName}".Trim(), x.LeaveTypeId, x.LeaveTypeDefinition?.Name ?? x.LeaveType, x.StartDate, x.EndDate, x.Days, x.Reason, x.Status, x.CurrentApproverId);
    private static LeaveBalanceDto MapBalance(LeaveBalance x) => new(x.Id, x.EmployeeId, x.LeaveTypeId, x.LeaveType?.Name, x.Year, x.OpeningBalance, x.Accrued, x.Used, x.Adjusted, x.Remaining);
    private static LeaveEntitlementDto MapEntitlement(LeaveEntitlement x) => new(x.Id, x.LeaveTypeId, x.LeaveType?.Name, x.PolicyGroup, x.EmploymentTypeId, x.GradeLevelId, x.EntitlementDays, x.AccrualRule, x.Year);
}
