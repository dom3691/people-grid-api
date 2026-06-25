using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Notifications.DTOs;
using PeopleGrid.Application.Features.Notifications.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/notification-templates")]
[Authorize]
public sealed class NotificationTemplatesController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Notification.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationTemplateDto>>>> List(CancellationToken cancellationToken)
    {
        var response = await notificationService.ListTemplatesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<NotificationTemplateDto>>.Ok(response));
    }

    [HttpPut("{key}")]
    [HasPermission("Notification.Manage")]
    public async Task<ActionResult<ApiResponse<NotificationTemplateDto>>> Update(string key, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var response = await notificationService.UpdateTemplateAsync(key, request, cancellationToken);
        return Ok(ApiResponse<NotificationTemplateDto>.Ok(response, "Notification template updated successfully"));
    }
}
