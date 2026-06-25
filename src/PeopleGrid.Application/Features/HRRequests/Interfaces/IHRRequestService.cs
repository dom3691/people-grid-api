using PeopleGrid.Application.Features.HRRequests.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.HRRequests.Interfaces;

public interface IHRRequestService
{
    Task<HRRequestDto> CreateAsync(CreateHRRequestRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<HRRequestListItemDto>> ListAsync(HRRequestListQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<HRRequestListItemDto>> ListMyAsync(HRRequestListQuery query, CancellationToken cancellationToken = default);
    Task<HRRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HRRequestDto> UpdateDraftAsync(Guid id, UpdateHRRequestRequest request, CancellationToken cancellationToken = default);
    Task<HRRequestDto> SubmitAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default);
    Task<HRRequestDto> CancelAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default);
    Task<HRRequestDto> CompleteAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HRRequestStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HRRequestAttachmentDto> UploadAttachmentAsync(Guid id, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
}
