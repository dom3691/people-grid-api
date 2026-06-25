using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Notifications.DTOs;
using PeopleGrid.Application.Features.Notifications.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Notification.View")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationDto>>>> List([FromQuery] NotificationListQuery query, CancellationToken cancellationToken)
    {
        var response = await notificationService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<NotificationDto>>.Ok(response));
    }

    [HttpGet("unread-count")]
    [HasPermission("Notification.View")]
    public async Task<ActionResult<ApiResponse<object>>> UnreadCount(CancellationToken cancellationToken)
    {
        var count = await notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { Count = count }));
    }

    [HttpPatch("{id:guid}/read")]
    [HasPermission("Notification.View")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notification marked as read"));
    }

    [HttpPatch("read-all")]
    [HasPermission("Notification.View")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Notifications marked as read"));
    }

    [HttpPost("send-test")]
    [HasPermission("Notification.Manage")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> SendTest(SendTestNotificationRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.SendTestAsync(request, cancellationToken);
        return Ok(ApiResponse<NotificationDto>.Ok(response, "Test notification sent"));
    }

    [HttpGet("delivery-log")]
    [HasPermission("Notification.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationDeliveryLogDto>>>> DeliveryLog([FromQuery] NotificationDeliveryLogQuery query, CancellationToken cancellationToken)
    {
        var response = await notificationService.ListDeliveryLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<NotificationDeliveryLogDto>>.Ok(response));
    }
}
