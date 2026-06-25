using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.HRRequests.DTOs;
using PeopleGrid.Application.Features.HRRequests.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/hr-requests")]
[Authorize]
public sealed class HRRequestsController(IHRRequestService hrRequestService) : ControllerBase
{
    [HttpPost]
    [HasPermission("HRRequest.Create")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> Create(CreateHRRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<HRRequestDto>.Ok(response, "HR request created successfully"));
    }

    [HttpGet]
    [HasPermission("HRRequest.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<HRRequestListItemDto>>>> List([FromQuery] HRRequestListQuery query, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<HRRequestListItemDto>>.Ok(response));
    }

    [HttpGet("my")]
    [HasPermission("HRRequest.Create")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<HRRequestListItemDto>>>> MyRequests([FromQuery] HRRequestListQuery query, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.ListMyAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<HRRequestListItemDto>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("HRRequest.View")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<HRRequestDto>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("HRRequest.Create")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> UpdateDraft(Guid id, UpdateHRRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.UpdateDraftAsync(id, request, cancellationToken);
        return Ok(ApiResponse<HRRequestDto>.Ok(response, "HR request updated successfully"));
    }

    [HttpPost("{id:guid}/submit")]
    [HasPermission("HRRequest.Create")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> Submit(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.SubmitAsync(id, request, cancellationToken);
        return Ok(ApiResponse<HRRequestDto>.Ok(response, "HR request submitted successfully"));
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission("HRRequest.Create")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> Cancel(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.CancelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<HRRequestDto>.Ok(response, "HR request cancelled successfully"));
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission("HRRequest.Manage")]
    public async Task<ActionResult<ApiResponse<HRRequestDto>>> Complete(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.CompleteAsync(id, request, cancellationToken);
        return Ok(ApiResponse<HRRequestDto>.Ok(response, "HR request completed successfully"));
    }

    [HttpGet("{id:guid}/history")]
    [HasPermission("HRRequest.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HRRequestStatusHistoryDto>>>> History(Guid id, CancellationToken cancellationToken)
    {
        var response = await hrRequestService.GetHistoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<HRRequestStatusHistoryDto>>.Ok(response));
    }

    [HttpPost("{id:guid}/attachments")]
    [HasPermission("HRRequest.Create")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<HRRequestAttachmentDto>>> UploadAttachment(Guid id, [FromForm] HRRequestAttachmentUploadForm form, CancellationToken cancellationToken)
    {
        await using var stream = form.File.OpenReadStream();
        var response = await hrRequestService.UploadAttachmentAsync(id, stream, form.File.FileName, form.File.ContentType, form.File.Length, cancellationToken);
        return Ok(ApiResponse<HRRequestAttachmentDto>.Ok(response, "Attachment uploaded successfully"));
    }
}

public sealed class HRRequestAttachmentUploadForm
{
    public IFormFile File { get; set; } = default!;
}
