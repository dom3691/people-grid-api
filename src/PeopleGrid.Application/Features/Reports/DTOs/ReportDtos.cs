using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Reports.DTOs;

public sealed record ReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? DepartmentId,
    Guid? EmployeeId,
    string? Status,
    string? LeaveType,
    string? PayrollPeriod,
    string? CandidateStatus,
    string? Rating,
    Guid? TrainingProgramId,
    int PageNumber = 1,
    int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record DashboardCardDto(string CardCode, string Title, string Module, string Scope, decimal Value, int DisplayOrder, string? DrillDownUrl);
public sealed record DepartmentHeadcountDto(Guid DepartmentId, string DepartmentName, int Headcount);
public sealed record PendingApprovalsDto(int HrRequests, int LeaveRequests, int ApprovalSteps, int Total);
public sealed record ReportRowDto(IReadOnlyDictionary<string, object?> Values);
public sealed record ReportResultDto(string ReportCode, string ReportName, IReadOnlyCollection<ReportRowDto> Items, int PageNumber, int PageSize, int TotalCount);
public sealed record ReportExportRequest(string ReportCode, string Format, ReportQuery Filters, bool ConfirmLargeExport = false);
public sealed record ReportExportJobDto(Guid Id, string ReportCode, string Format, string Status, DateTime RequestedAt, DateTime? CompletedAt, string? FileName, string? StorageKey);

