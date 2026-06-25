using PeopleGrid.Application.Features.ExitManagement.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.ExitManagement.Interfaces;

public interface IExitManagementService
{
    Task<ExitCaseDto> SubmitResignationAsync(SubmitResignationRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<ExitCaseDto>> ListAsync(ExitCaseQuery query, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> ApproveAsync(Guid id, ExitDecisionRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> RejectAsync(Guid id, ExitDecisionRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> AddClearanceItemAsync(Guid id, AddExitClearanceItemRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> CompleteClearanceItemAsync(Guid id, CompleteExitClearanceItemRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> RecordHandoverAsync(Guid id, ExitHandoverRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> SubmitExitInterviewAsync(Guid id, ExitInterviewRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> UpdateFinalSettlementStatusAsync(Guid id, UpdateFinalSettlementStatusRequest request, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> DeactivateAccountAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExitCaseDto> CloseAsync(Guid id, CancellationToken cancellationToken = default);
}

