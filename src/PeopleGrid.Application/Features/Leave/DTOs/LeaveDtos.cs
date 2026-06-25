using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Leave.DTOs;

public sealed record LeaveRequestListQuery(string? Status, Guid? LeaveTypeId, Guid? EmployeeId, Guid? DepartmentId, DateOnly? FromDate, DateOnly? ToDate, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record CreateLeaveRequestRequest(Guid EmployeeId, Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason);
public sealed record LeaveDecisionRequest(string? Comments);
public sealed record LeaveEntitlementRequest(Guid LeaveTypeId, string? PolicyGroup, Guid? EmploymentTypeId, Guid? GradeLevelId, decimal EntitlementDays, string? AccrualRule, int Year);

public sealed record LeaveRequestDto(Guid Id, Guid EmployeeId, string? EmployeeName, Guid? LeaveTypeId, string LeaveType, DateTime StartDate, DateTime EndDate, decimal Days, string? Reason, string Status, Guid? CurrentApproverId);
public sealed record LeaveBalanceDto(Guid Id, Guid EmployeeId, Guid LeaveTypeId, string? LeaveType, int Year, decimal OpeningBalance, decimal Accrued, decimal Used, decimal Adjusted, decimal Remaining);
public sealed record LeaveHistoryDto(Guid Id, Guid LeaveTypeId, string LeaveType, DateTime StartDate, DateTime EndDate, decimal Days, string Status);
public sealed record LeaveCalendarItemDto(Guid LeaveRequestId, Guid EmployeeId, string? EmployeeName, DateOnly Date, string Status, string LeaveType);
public sealed record LeaveEntitlementDto(Guid Id, Guid LeaveTypeId, string? LeaveType, string? PolicyGroup, Guid? EmploymentTypeId, Guid? GradeLevelId, decimal EntitlementDays, string? AccrualRule, int Year);
