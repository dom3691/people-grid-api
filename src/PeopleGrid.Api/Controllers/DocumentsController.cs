using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.EmployeeDocuments.DTOs;
using PeopleGrid.Application.Features.EmployeeDocuments.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Authorize]
public sealed class DocumentsController(IEmployeeDocumentService documentService) : ControllerBase
{
    [HttpGet("api/documents/{id:guid}")]
    [HasPermission("EmployeeDocument.View")]
    public async Task<ActionResult<ApiResponse<EmployeeDocumentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await documentService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeDocumentDto>.Ok(response));
    }

    [HttpGet("api/documents/{id:guid}/download")]
    [HasPermission("EmployeeDocument.View")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var response = await documentService.DownloadAsync(id, cancellationToken);
        return File(response.Content, response.ContentType, response.FileName);
    }

    [HttpDelete("api/documents/{id:guid}")]
    [HasPermission("EmployeeDocument.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> Archive(Guid id, CancellationToken cancellationToken)
    {
        await documentService.ArchiveAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Document archived successfully"));
    }

    [HttpPost("api/documents/{id:guid}/verify")]
    [HasPermission("EmployeeDocument.Verify")]
    public async Task<ActionResult<ApiResponse<EmployeeDocumentDto>>> Verify(Guid id, VerifyDocumentRequest request, CancellationToken cancellationToken)
    {
        var response = await documentService.VerifyAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDocumentDto>.Ok(response, "Document verified successfully"));
    }

    [HttpPost("api/documents/{id:guid}/reject")]
    [HasPermission("EmployeeDocument.Verify")]
    public async Task<ActionResult<ApiResponse<EmployeeDocumentDto>>> Reject(Guid id, RejectDocumentRequest request, CancellationToken cancellationToken)
    {
        var response = await documentService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDocumentDto>.Ok(response, "Document rejected successfully"));
    }

    [HttpGet("api/documents/expiring")]
    [HasPermission("EmployeeDocument.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeDocumentDto>>>> Expiring([FromQuery] ExpiringDocumentsQuery query, CancellationToken cancellationToken)
    {
        var response = await documentService.ListExpiringAsync(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<EmployeeDocumentDto>>.Ok(response));
    }

    [HttpGet("api/document-types")]
    [HasPermission("EmployeeDocument.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DocumentTypeDto>>>> DocumentTypes(CancellationToken cancellationToken)
    {
        var response = await documentService.ListDocumentTypesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<DocumentTypeDto>>.Ok(response));
    }
}
