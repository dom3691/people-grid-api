using PeopleGrid.Application.Features.Notifications.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    Task<PaginatedResponse<NotificationDto>> ListAsync(NotificationListQuery query, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto> UpdateTemplateAsync(string key, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default);
    Task<NotificationDto> SendTestAsync(SendTestNotificationRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<NotificationDeliveryLogDto>> ListDeliveryLogsAsync(NotificationDeliveryLogQuery query, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateAsync(Guid recipientUserId, string type, string title, string message, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken cancellationToken = default);
}
