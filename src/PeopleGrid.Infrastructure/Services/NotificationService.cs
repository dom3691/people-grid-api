using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Notifications.DTOs;
using PeopleGrid.Application.Features.Notifications.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class NotificationService(IApplicationDbContext dbContext, ICurrentUserService currentUser, IEmailService emailService) : INotificationService
{
    public async Task<PaginatedResponse<NotificationDto>> ListAsync(NotificationListQuery query, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserGuid();
        var source = dbContext.Notifications.AsNoTracking().Where(x => x.RecipientUserId == userId);
        if (query.IsRead is not null) source = source.Where(x => x.IsRead == query.IsRead);
        if (!string.IsNullOrWhiteSpace(query.Type)) source = source.Where(x => x.Type == query.Type);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var items = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).Select(x => Map(x)).ToListAsync(cancellationToken);
        return new PaginatedResponse<NotificationDto>(items, page.PageNumber, page.Take, total);
    }

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) => dbContext.Notifications.CountAsync(x => x.RecipientUserId == CurrentUserGuid() && !x.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == CurrentUserGuid(), cancellationToken) ?? throw new NotFoundException("Notification was not found.");
        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserGuid();
        var notifications = await dbContext.Notifications.Where(x => x.RecipientUserId == userId && !x.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in notifications) notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<NotificationTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.NotificationTemplates.AsNoTracking().OrderBy(x => x.TemplateKey).ThenBy(x => x.Channel).Select(x => new NotificationTemplateDto(x.Id, x.TemplateKey, x.Channel, x.Subject, x.Body, x.IsActive)).ToListAsync(cancellationToken);
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(string key, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.NotificationTemplates.FirstOrDefaultAsync(x => x.TemplateKey == key, cancellationToken);
        if (template is null)
        {
            template = new NotificationTemplate { TemplateKey = key, Channel = "InApp" };
            dbContext.NotificationTemplates.Add(template);
        }
        template.Subject = request.Subject?.Trim();
        template.Body = request.Body.Trim();
        template.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new NotificationTemplateDto(template.Id, template.TemplateKey, template.Channel, template.Subject, template.Body, template.IsActive);
    }

    public async Task<NotificationDto> SendTestAsync(SendTestNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RecipientUserId && x.IsActive, cancellationToken) ?? throw new BusinessRuleException("Recipient is invalid.");
        var template = await dbContext.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.TemplateKey == request.TemplateKey && x.Channel == request.Channel && x.IsActive, cancellationToken) ?? throw new BusinessRuleException("Template key is invalid.");
        var notification = await CreateAsync(user.Id, request.TemplateKey, template.Subject ?? "PeopleGrid notification", template.Body, cancellationToken: cancellationToken);
        if (request.Channel == "Email")
        {
            await TrySendEmailAsync(notification.Id, user.Email, template.Subject ?? notification.Title, template.Body, cancellationToken);
        }
        return notification;
    }

    public async Task<PaginatedResponse<NotificationDeliveryLogDto>> ListDeliveryLogsAsync(NotificationDeliveryLogQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.NotificationDeliveryLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Channel)) source = source.Where(x => x.Channel == query.Channel);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.FromDate is not null) source = source.Where(x => x.AttemptedAt >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.AttemptedAt <= query.ToDate);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var items = await source.OrderByDescending(x => x.AttemptedAt).Skip(page.Skip).Take(page.Take).Select(x => new NotificationDeliveryLogDto(x.Id, x.NotificationId, x.Channel, x.RecipientAddress, x.Status, x.ProviderMessageId, x.ErrorMessage, x.AttemptedAt)).ToListAsync(cancellationToken);
        return new PaginatedResponse<NotificationDeliveryLogDto>(items, page.PageNumber, page.Take, total);
    }

    public async Task<NotificationDto> CreateAsync(Guid recipientUserId, string type, string title, string message, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Id == recipientUserId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Recipient is invalid.");
        var notification = new Notification { RecipientUserId = recipientUserId, Type = type, Title = title, Message = message, RelatedEntityType = relatedEntityType, RelatedEntityId = relatedEntityId, IsRead = false };
        dbContext.Notifications.Add(notification);
        dbContext.NotificationDeliveryLogs.Add(new NotificationDeliveryLog { NotificationId = notification.Id, Channel = "InApp", RecipientAddress = recipientUserId.ToString(), Status = "Delivered", AttemptedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    private async Task TrySendEmailAsync(Guid notificationId, string email, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            await emailService.SendAsync(email, subject, body, cancellationToken);
            dbContext.NotificationDeliveryLogs.Add(new NotificationDeliveryLog { NotificationId = notificationId, Channel = "Email", RecipientAddress = email, Status = "Delivered", AttemptedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            dbContext.NotificationDeliveryLogs.Add(new NotificationDeliveryLog { NotificationId = notificationId, Channel = "Email", RecipientAddress = email, Status = "Failed", ErrorMessage = ex.Message, AttemptedAt = DateTime.UtcNow });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
    private static NotificationDto Map(Notification x) => new(x.Id, x.Type, x.Title, x.Message, x.RelatedEntityType, x.RelatedEntityId, x.IsRead, x.CreatedAt);
}
