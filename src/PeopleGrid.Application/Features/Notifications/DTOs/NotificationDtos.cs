using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Notifications.DTOs;

public sealed record NotificationListQuery(bool? IsRead, string? Type, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record NotificationDto(Guid Id, string Type, string Title, string Message, string? RelatedEntityType, Guid? RelatedEntityId, bool IsRead, DateTime CreatedAt);
public sealed record NotificationTemplateDto(Guid Id, string TemplateKey, string Channel, string? Subject, string Body, bool IsActive);
public sealed record UpdateNotificationTemplateRequest(string? Subject, string Body, bool IsActive);
public sealed record SendTestNotificationRequest(Guid RecipientUserId, string TemplateKey, string Channel);
public sealed record NotificationDeliveryLogQuery(string? Channel, string? Status, DateTime? FromDate, DateTime? ToDate, int PageNumber = 1, int PageSize = 20)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}
public sealed record NotificationDeliveryLogDto(Guid Id, Guid NotificationId, string Channel, string RecipientAddress, string Status, string? ProviderMessageId, string? ErrorMessage, DateTime AttemptedAt);
