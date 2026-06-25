using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Attendance.DTOs;

public sealed record ClockEventRequest(Guid EmployeeId, DateTime EventTime, string Source = "Manual", string? DeviceId = null, string? AccessSystemRef = null, decimal? GpsLatitude = null, decimal? GpsLongitude = null);
public sealed record AttendanceQuery(Guid? EmployeeId, Guid? DepartmentId, DateOnly? Date, DateOnly? FromDate, DateOnly? ToDate, string? Status, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}
public sealed record MonthlySummaryQuery(int Year, int Month, Guid? EmployeeId, Guid? DepartmentId);
public sealed record AttendanceCorrectionRequestDto(Guid AttendanceRecordId, DateTime? RequestedClockIn, DateTime? RequestedClockOut, string Reason);
public sealed record AttendanceDecisionRequest(string? Comments);
public sealed record ImportAttendanceEventRequest(Guid EmployeeId, DateTime EventTime, string EventType, string Source, string? DeviceId, string? AccessSystemRef, decimal? GpsLatitude, decimal? GpsLongitude);

public sealed record AttendanceRecordDto(Guid Id, Guid EmployeeId, string? EmployeeName, DateOnly AttendanceDate, DateTime? ClockInAt, DateTime? ClockOutAt, string Status, int LateMinutes, int OvertimeMinutes, string Source);
public sealed record AttendanceSummaryDto(Guid EmployeeId, string? EmployeeName, int PresentDays, int LateDays, int AbsentDays, int OvertimeMinutes);
public sealed record AttendanceCorrectionDto(Guid Id, Guid AttendanceRecordId, DateTime? RequestedClockIn, DateTime? RequestedClockOut, string Reason, string Status);
