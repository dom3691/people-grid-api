using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Reports.DTOs;
using PeopleGrid.Application.Features.Reports.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("employees")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Employees([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetEmployeeReportAsync(query, ct)));
    [HttpGet("leave")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Leave([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetLeaveReportAsync(query, ct)));
    [HttpGet("attendance")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Attendance([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetAttendanceReportAsync(query, ct)));
    [HttpGet("payroll")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Payroll([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetPayrollReportAsync(query, ct)));
    [HttpGet("recruitment")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Recruitment([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetRecruitmentReportAsync(query, ct)));
    [HttpGet("training")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Training([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetTrainingReportAsync(query, ct)));
    [HttpGet("exit")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Exit([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetExitReportAsync(query, ct)));
    [HttpGet("department")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Department([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetDepartmentReportAsync(query, ct)));
    [HttpGet("audit")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Audit([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetAuditReportAsync(query, ct)));
    [HttpGet("performance")][HasPermission("Report.View")] public async Task<ActionResult<ApiResponse<ReportResultDto>>> Performance([FromQuery] ReportQuery query, CancellationToken ct) => Ok(ApiResponse<ReportResultDto>.Ok(await reportService.GetPerformanceReportAsync(query, ct)));
    [HttpPost("export")][HasPermission("Report.Export")] public async Task<ActionResult<ApiResponse<ReportExportJobDto>>> Export(ReportExportRequest request, CancellationToken ct) => Ok(ApiResponse<ReportExportJobDto>.Ok(await reportService.ExportAsync(request, ct), "Report export requested"));
}

