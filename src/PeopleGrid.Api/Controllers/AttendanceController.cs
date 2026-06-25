using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Attendance.DTOs;
using PeopleGrid.Application.Features.Attendance.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpPost("clock-in")][HasPermission("Attendance.Clock")] public async Task<ActionResult<ApiResponse<AttendanceRecordDto>>> ClockIn(ClockEventRequest request, CancellationToken ct) => Ok(ApiResponse<AttendanceRecordDto>.Ok(await attendanceService.ClockInAsync(request, ct), "Clock-in recorded"));
    [HttpPost("clock-out")][HasPermission("Attendance.Clock")] public async Task<ActionResult<ApiResponse<AttendanceRecordDto>>> ClockOut(ClockEventRequest request, CancellationToken ct) => Ok(ApiResponse<AttendanceRecordDto>.Ok(await attendanceService.ClockOutAsync(request, ct), "Clock-out recorded"));
    [HttpGet("daily")][HasPermission("Attendance.View")] public async Task<ActionResult<ApiResponse<PaginatedResponse<AttendanceRecordDto>>>> Daily([FromQuery] AttendanceQuery query, CancellationToken ct) => Ok(ApiResponse<PaginatedResponse<AttendanceRecordDto>>.Ok(await attendanceService.DailyAsync(query, ct)));
    [HttpGet("monthly-summary")][HasPermission("Attendance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceSummaryDto>>>> Monthly([FromQuery] MonthlySummaryQuery query, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<AttendanceSummaryDto>>.Ok(await attendanceService.MonthlySummaryAsync(query, ct)));
    [HttpGet("late-coming")][HasPermission("Attendance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>>> Late([FromQuery] AttendanceQuery query, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>.Ok(await attendanceService.LateComingAsync(query, ct)));
    [HttpGet("absence")][HasPermission("Attendance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>>> Absence([FromQuery] AttendanceQuery query, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>.Ok(await attendanceService.AbsenceAsync(query, ct)));
    [HttpGet("overtime")][HasPermission("Attendance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>>> Overtime([FromQuery] AttendanceQuery query, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<AttendanceRecordDto>>.Ok(await attendanceService.OvertimeAsync(query, ct)));
    [HttpPost("corrections")][HasPermission("Attendance.Clock")] public async Task<ActionResult<ApiResponse<AttendanceCorrectionDto>>> Correct(AttendanceCorrectionRequestDto request, CancellationToken ct) => Ok(ApiResponse<AttendanceCorrectionDto>.Ok(await attendanceService.SubmitCorrectionAsync(request, ct), "Correction request submitted"));
    [HttpPost("corrections/{id:guid}/approve")][HasPermission("Attendance.Manage")] public async Task<ActionResult<ApiResponse<AttendanceCorrectionDto>>> Approve(Guid id, AttendanceDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<AttendanceCorrectionDto>.Ok(await attendanceService.ApproveCorrectionAsync(id, request, ct), "Correction approved"));
    [HttpPost("corrections/{id:guid}/reject")][HasPermission("Attendance.Manage")] public async Task<ActionResult<ApiResponse<AttendanceCorrectionDto>>> Reject(Guid id, AttendanceDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<AttendanceCorrectionDto>.Ok(await attendanceService.RejectCorrectionAsync(id, request, ct), "Correction rejected"));
    [HttpPost("import-events")][HasPermission("Attendance.Manage")] public async Task<ActionResult<ApiResponse<object>>> Import(IReadOnlyCollection<ImportAttendanceEventRequest> requests, CancellationToken ct) => Ok(ApiResponse<object>.Ok(new { Imported = await attendanceService.ImportEventsAsync(requests, ct) }));
}
