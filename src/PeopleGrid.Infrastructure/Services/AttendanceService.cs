using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Attendance.DTOs;
using PeopleGrid.Application.Features.Attendance.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class AttendanceService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IAttendanceService
{
    private static readonly TimeOnly DefaultStart = new(8, 0);
    private static readonly TimeOnly DefaultEnd = new(17, 0);
    private const int DefaultGraceMinutes = 15;
    private const int DefaultOvertimeThresholdMinutes = 30;

    public Task<AttendanceRecordDto> ClockInAsync(ClockEventRequest request, CancellationToken cancellationToken = default) => RecordEventAsync(request, "ClockIn", cancellationToken);
    public Task<AttendanceRecordDto> ClockOutAsync(ClockEventRequest request, CancellationToken cancellationToken = default) => RecordEventAsync(request, "ClockOut", cancellationToken);

    public async Task<PaginatedResponse<AttendanceRecordDto>> DailyAsync(AttendanceQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyFilters(LoadRecords(), query);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.AttendanceDate).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<AttendanceRecordDto>(rows.Select(Map).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<IReadOnlyCollection<AttendanceSummaryDto>> MonthlySummaryAsync(MonthlySummaryQuery query, CancellationToken cancellationToken = default)
    {
        var start = new DateOnly(query.Year, query.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var records = await LoadRecords().Where(x => x.AttendanceDate >= start && x.AttendanceDate <= end)
            .Where(x => query.EmployeeId == null || x.EmployeeId == query.EmployeeId)
            .Where(x => query.DepartmentId == null || x.Employee!.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId)
            .ToListAsync(cancellationToken);
        return records.GroupBy(x => x.EmployeeId).Select(g => new AttendanceSummaryDto(g.Key, EmployeeName(g.First().Employee), g.Count(x => x.Status == "Present"), g.Count(x => x.LateMinutes > 0), g.Count(x => x.Status == "Absent"), g.Sum(x => x.OvertimeMinutes))).ToList();
    }

    public async Task<IReadOnlyCollection<AttendanceRecordDto>> LateComingAsync(AttendanceQuery query, CancellationToken cancellationToken = default) => (await ApplyFilters(LoadRecords(), query).Where(x => x.LateMinutes > 0).ToListAsync(cancellationToken)).Select(Map).ToList();
    public async Task<IReadOnlyCollection<AttendanceRecordDto>> AbsenceAsync(AttendanceQuery query, CancellationToken cancellationToken = default) => (await ApplyFilters(LoadRecords(), query).Where(x => x.Status == "Absent").ToListAsync(cancellationToken)).Select(Map).ToList();
    public async Task<IReadOnlyCollection<AttendanceRecordDto>> OvertimeAsync(AttendanceQuery query, CancellationToken cancellationToken = default) => (await ApplyFilters(LoadRecords(), query).Where(x => x.OvertimeMinutes > 0).ToListAsync(cancellationToken)).Select(Map).ToList();

    public async Task<AttendanceCorrectionDto> SubmitCorrectionAsync(AttendanceCorrectionRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken) ?? throw new NotFoundException("Attendance record was not found.");
        var correction = new AttendanceCorrectionRequest { AttendanceRecordId = record.Id, RequestedClockIn = request.RequestedClockIn, RequestedClockOut = request.RequestedClockOut, Reason = request.Reason.Trim(), Status = "Pending" };
        dbContext.AttendanceCorrectionRequests.Add(correction);
        AddAudit("Attendance", "SubmitCorrection", correction.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCorrection(correction);
    }

    public Task<AttendanceCorrectionDto> ApproveCorrectionAsync(Guid id, AttendanceDecisionRequest request, CancellationToken cancellationToken = default) => DecideCorrectionAsync(id, "Approved", request.Comments, cancellationToken);
    public Task<AttendanceCorrectionDto> RejectCorrectionAsync(Guid id, AttendanceDecisionRequest request, CancellationToken cancellationToken = default) => DecideCorrectionAsync(id, "Rejected", request.Comments, cancellationToken);

    public async Task<int> ImportEventsAsync(IReadOnlyCollection<ImportAttendanceEventRequest> requests, CancellationToken cancellationToken = default)
    {
        var imported = 0;
        foreach (var request in requests)
        {
            var exists = await dbContext.AttendanceEvents.AnyAsync(x => x.EmployeeId == request.EmployeeId && x.EventTime == request.EventTime && x.EventType == request.EventType && x.Source == request.Source, cancellationToken);
            if (exists) continue;
            await RecordEventAsync(new ClockEventRequest(request.EmployeeId, request.EventTime, request.Source, request.DeviceId, request.AccessSystemRef, request.GpsLatitude, request.GpsLongitude), request.EventType, cancellationToken);
            imported++;
        }
        return imported;
    }

    private async Task<AttendanceRecordDto> RecordEventAsync(ClockEventRequest request, string eventType, CancellationToken cancellationToken)
    {
        await ValidateEmployeeAsync(request.EmployeeId, cancellationToken);
        if (await dbContext.AttendanceEvents.AnyAsync(x => x.EmployeeId == request.EmployeeId && x.EventTime == request.EventTime && x.EventType == eventType, cancellationToken))
            throw new BusinessRuleException("Duplicate attendance event detected.");

        var date = DateOnly.FromDateTime(request.EventTime);
        var record = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date, cancellationToken);
        if (record is null)
        {
            record = new AttendanceRecord { EmployeeId = request.EmployeeId, AttendanceDate = date, Status = "Present", Source = request.Source };
            dbContext.AttendanceRecords.Add(record);
        }

        dbContext.AttendanceEvents.Add(new AttendanceEvent { EmployeeId = request.EmployeeId, EventTime = request.EventTime, EventType = eventType, Source = request.Source, DeviceId = request.DeviceId, AccessSystemRef = request.AccessSystemRef, GpsLatitude = request.GpsLatitude, GpsLongitude = request.GpsLongitude });
        if (eventType == "ClockIn")
        {
            if (record.ClockInAt is not null) throw new BusinessRuleException("Clock-in already exists for this date.");
            record.ClockInAt = request.EventTime;
            record.LateMinutes = CalculateLateMinutes(request.EventTime);
        }
        else
        {
            if (record.ClockOutAt is not null) throw new BusinessRuleException("Clock-out already exists for this date.");
            record.ClockOutAt = request.EventTime;
            record.OvertimeMinutes = CalculateOvertimeMinutes(request.EventTime);
        }
        AddAudit("Attendance", eventType, record.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(await LoadRecords().FirstAsync(x => x.Id == record.Id, cancellationToken));
    }

    private async Task<AttendanceCorrectionDto> DecideCorrectionAsync(Guid id, string decision, string? comments, CancellationToken cancellationToken)
    {
        if (!currentUser.Permissions.Contains("Attendance.Manage")) throw new ForbiddenException("Attendance approval permission is required.");
        var correction = await dbContext.AttendanceCorrectionRequests.Include(x => x.AttendanceRecord).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Correction request was not found.");
        if (correction.Status != "Pending") throw new BusinessRuleException("Correction request is not pending.");
        await using var tx = await BeginTransactionAsync(cancellationToken);
        correction.Status = decision;
        if (decision == "Approved" && correction.AttendanceRecord is not null)
        {
            correction.AttendanceRecord.ClockInAt = correction.RequestedClockIn ?? correction.AttendanceRecord.ClockInAt;
            correction.AttendanceRecord.ClockOutAt = correction.RequestedClockOut ?? correction.AttendanceRecord.ClockOutAt;
            if (correction.AttendanceRecord.ClockInAt is not null) correction.AttendanceRecord.LateMinutes = CalculateLateMinutes(correction.AttendanceRecord.ClockInAt.Value);
            if (correction.AttendanceRecord.ClockOutAt is not null) correction.AttendanceRecord.OvertimeMinutes = CalculateOvertimeMinutes(correction.AttendanceRecord.ClockOutAt.Value);
        }
        dbContext.AttendanceApprovalActions.Add(new AttendanceApprovalAction { CorrectionRequestId = correction.Id, ActorUserId = CurrentUserGuid(), Decision = decision, Comments = comments?.Trim(), DecidedAt = DateTime.UtcNow });
        AddAudit("Attendance", decision + "Correction", correction.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (tx is not null) await tx.CommitAsync(cancellationToken);
        return MapCorrection(correction);
    }

    private IQueryable<AttendanceRecord> LoadRecords() => dbContext.AttendanceRecords.AsNoTracking().Include(x => x.Employee).ThenInclude(x => x!.PersonalInfo).Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo);
    private static IQueryable<AttendanceRecord> ApplyFilters(IQueryable<AttendanceRecord> source, AttendanceQuery query)
    {
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee!.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (query.Date is not null) source = source.Where(x => x.AttendanceDate == query.Date);
        if (query.FromDate is not null) source = source.Where(x => x.AttendanceDate >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.AttendanceDate <= query.ToDate);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        return source;
    }
    private async Task ValidateEmployeeAsync(Guid employeeId, CancellationToken ct) { if (!await dbContext.Employees.AnyAsync(x => x.Id == employeeId, ct)) throw new BusinessRuleException("Employee is invalid."); }
    private static int CalculateLateMinutes(DateTime clockIn) { var allowed = clockIn.Date.Add(DefaultStart.ToTimeSpan()).AddMinutes(DefaultGraceMinutes); return clockIn > allowed ? (int)(clockIn - allowed).TotalMinutes : 0; }
    private static int CalculateOvertimeMinutes(DateTime clockOut) { var threshold = clockOut.Date.Add(DefaultEnd.ToTimeSpan()).AddMinutes(DefaultOvertimeThresholdMinutes); return clockOut > threshold ? (int)(clockOut - threshold).TotalMinutes : 0; }
    private static string? EmployeeName(Employee? employee) => employee?.PersonalInfo is null ? null : $"{employee.PersonalInfo.FirstName} {employee.PersonalInfo.LastName}".Trim();
    private static AttendanceRecordDto Map(AttendanceRecord x) => new(x.Id, x.EmployeeId, EmployeeName(x.Employee), x.AttendanceDate, x.ClockInAt, x.ClockOutAt, x.Status, x.LateMinutes, x.OvertimeMinutes, x.Source);
    private static AttendanceCorrectionDto MapCorrection(AttendanceCorrectionRequest x) => new(x.Id, x.AttendanceRecordId, x.RequestedClockIn, x.RequestedClockOut, x.Reason, x.Status);
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
    private void AddAudit(string module, string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "Attendance", EntityId = id.ToString(), Outcome = "Success" });
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct) => dbContext is DbContext ef && ef.Database.CurrentTransaction is null ? await ef.Database.BeginTransactionAsync(ct) : null;
}
