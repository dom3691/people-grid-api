using PeopleGrid.Application.Features.Reports.DTOs;

namespace PeopleGrid.Application.Features.Reports.Interfaces;

public interface IReportService
{
    Task<IReadOnlyCollection<DashboardCardDto>> GetDashboardCardsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DepartmentHeadcountDto>> GetDepartmentHeadcountAsync(CancellationToken cancellationToken = default);
    Task<PendingApprovalsDto> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetEmployeeReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetLeaveReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetAttendanceReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetPayrollReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetRecruitmentReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetTrainingReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetExitReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetDepartmentReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetAuditReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportResultDto> GetPerformanceReportAsync(ReportQuery query, CancellationToken cancellationToken = default);
    Task<ReportExportJobDto> ExportAsync(ReportExportRequest request, CancellationToken cancellationToken = default);
}

