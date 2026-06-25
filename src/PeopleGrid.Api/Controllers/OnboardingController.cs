using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Onboarding.DTOs;
using PeopleGrid.Application.Features.Onboarding.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public sealed class OnboardingController(IOnboardingService onboardingService) : ControllerBase
{
    [HttpPost("plans")][HasPermission("Onboarding.Manage")] public async Task<ActionResult<ApiResponse<OnboardingPlanDto>>> Create(CreateOnboardingPlanRequest request, CancellationToken ct) => Ok(ApiResponse<OnboardingPlanDto>.Ok(await onboardingService.CreatePlanAsync(request, ct), "Onboarding plan created"));
    [HttpGet("plans")][HasPermission("Onboarding.View")] public async Task<ActionResult<ApiResponse<PaginatedResponse<OnboardingPlanDto>>>> List([FromQuery] OnboardingPlanQuery query, CancellationToken ct) => Ok(ApiResponse<PaginatedResponse<OnboardingPlanDto>>.Ok(await onboardingService.ListPlansAsync(query, ct)));
    [HttpGet("plans/{id:guid}")][HasPermission("Onboarding.View")] public async Task<ActionResult<ApiResponse<OnboardingPlanDto>>> Detail(Guid id, CancellationToken ct) => Ok(ApiResponse<OnboardingPlanDto>.Ok(await onboardingService.GetPlanAsync(id, ct)));
    [HttpPut("plans/{id:guid}")][HasPermission("Onboarding.Manage")] public async Task<ActionResult<ApiResponse<OnboardingPlanDto>>> Update(Guid id, UpdateOnboardingPlanRequest request, CancellationToken ct) => Ok(ApiResponse<OnboardingPlanDto>.Ok(await onboardingService.UpdatePlanAsync(id, request, ct), "Onboarding plan updated"));
    [HttpPost("plans/{id:guid}/tasks")][HasPermission("Onboarding.Manage")] public async Task<ActionResult<ApiResponse<OnboardingTaskDto>>> AddTask(Guid id, AddOnboardingTaskRequest request, CancellationToken ct) => Ok(ApiResponse<OnboardingTaskDto>.Ok(await onboardingService.AddTaskAsync(id, request, ct), "Task added"));
    [HttpPatch("tasks/{id:guid}/complete")][HasPermission("Onboarding.View")] public async Task<ActionResult<ApiResponse<OnboardingTaskDto>>> Complete(Guid id, CancellationToken ct) => Ok(ApiResponse<OnboardingTaskDto>.Ok(await onboardingService.CompleteTaskAsync(id, ct), "Task completed"));
    [HttpPatch("tasks/{id:guid}/reopen")][HasPermission("Onboarding.Manage")] public async Task<ActionResult<ApiResponse<OnboardingTaskDto>>> Reopen(Guid id, ReopenTaskRequest request, CancellationToken ct) => Ok(ApiResponse<OnboardingTaskDto>.Ok(await onboardingService.ReopenTaskAsync(id, request, ct), "Task reopened"));
    [HttpPost("{id:guid}/documents")][HasPermission("Onboarding.View")][Consumes("multipart/form-data")] public async Task<ActionResult<ApiResponse<object>>> Document(Guid id, [FromForm] OnboardingDocumentUploadForm form, CancellationToken ct) { await using var s = form.File.OpenReadStream(); await onboardingService.SubmitDocumentAsync(id, form.DocumentTypeId, s, form.File.FileName, ct); return Ok(ApiResponse<object>.Ok(null, "Document submitted")); }
    [HttpPost("{id:guid}/send-welcome-email")][HasPermission("Onboarding.Manage")] public async Task<ActionResult<ApiResponse<object>>> Welcome(Guid id, CancellationToken ct) { await onboardingService.SendWelcomeEmailAsync(id, ct); return Ok(ApiResponse<object>.Ok(null, "Welcome email sent")); }
    [HttpPost("{id:guid}/acknowledge-policy")][HasPermission("Onboarding.View")] public async Task<ActionResult<ApiResponse<object>>> Acknowledge(Guid id, AcknowledgePolicyRequest request, CancellationToken ct) { await onboardingService.AcknowledgePolicyAsync(id, request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct); return Ok(ApiResponse<object>.Ok(null, "Policy acknowledged")); }
    [HttpGet("{id:guid}/progress")][HasPermission("Onboarding.View")] public async Task<ActionResult<ApiResponse<OnboardingProgressDto>>> Progress(Guid id, CancellationToken ct) => Ok(ApiResponse<OnboardingProgressDto>.Ok(await onboardingService.GetProgressAsync(id, ct)));
}

public sealed class OnboardingDocumentUploadForm
{
    public Guid? DocumentTypeId { get; set; }
    public IFormFile File { get; set; } = default!;
}
