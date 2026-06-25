using PeopleGrid.Application.Features.Attendance.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Attendance.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordDto> ClockInAsync(ClockEventRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceRecordDto> ClockOutAsync(ClockEventRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<AttendanceRecordDto>> DailyAsync(AttendanceQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceSummaryDto>> MonthlySummaryAsync(MonthlySummaryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceRecordDto>> LateComingAsync(AttendanceQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceRecordDto>> AbsenceAsync(AttendanceQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttendanceRecordDto>> OvertimeAsync(AttendanceQuery query, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionDto> SubmitCorrectionAsync(AttendanceCorrectionRequestDto request, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionDto> ApproveCorrectionAsync(Guid id, AttendanceDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceCorrectionDto> RejectCorrectionAsync(Guid id, AttendanceDecisionRequest request, CancellationToken cancellationToken = default);
    Task<int> ImportEventsAsync(IReadOnlyCollection<ImportAttendanceEventRequest> requests, CancellationToken cancellationToken = default);
}
