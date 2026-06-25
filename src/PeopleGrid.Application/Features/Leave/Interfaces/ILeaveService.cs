using PeopleGrid.Application.Features.Leave.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Leave.Interfaces;

public interface ILeaveService
{
    Task<LeaveRequestDto> CreateRequestAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<LeaveRequestDto>> ListRequestsAsync(LeaveRequestListQuery query, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> GetRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> SubmitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> ApproveAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> RejectAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default);
    Task<LeaveRequestDto> CancelAsync(Guid id, LeaveDecisionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaveCalendarItemDto>> GetCalendarAsync(Guid? departmentId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<LeaveEntitlementDto> CreateEntitlementAsync(LeaveEntitlementRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<LeaveEntitlementDto>> ListEntitlementsAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<LeaveEntitlementDto> UpdateEntitlementAsync(Guid id, LeaveEntitlementRequest request, CancellationToken cancellationToken = default);
}
