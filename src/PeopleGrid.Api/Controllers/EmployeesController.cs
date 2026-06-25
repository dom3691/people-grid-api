using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.EmployeeDocuments.DTOs;
using PeopleGrid.Application.Features.EmployeeDocuments.Interfaces;
using PeopleGrid.Application.Features.Employees.DTOs;
using PeopleGrid.Application.Features.Employees.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmployeesController(IEmployeeService employeeService, IEmployeeDocumentService documentService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Employee.View")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EmployeeListItemDto>>>> List([FromQuery] EmployeeListQuery query, CancellationToken cancellationToken)
    {
        var response = await employeeService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<EmployeeListItemDto>>.Ok(response));
    }

    [HttpPost]
    [HasPermission("Employee.Create")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<EmployeeDetailsDto>.Ok(response, "Employee created successfully"));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Employee.View")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await employeeService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response, "Employee updated successfully"));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> ChangeStatus(Guid id, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response, "Employee status updated successfully"));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission("Employee.Deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, DeactivateEmployeeRequest request, CancellationToken cancellationToken)
    {
        await employeeService.DeactivateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Employee deactivated successfully"));
    }

    [HttpPost("generate-number")]
    [HasPermission("Employee.Create")]
    public async Task<ActionResult<ApiResponse<GenerateEmployeeNumberResponse>>> GenerateNumber(GenerateEmployeeNumberRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.GenerateNumberAsync(request, cancellationToken);
        return Ok(ApiResponse<GenerateEmployeeNumberResponse>.Ok(response));
    }

    [HttpPut("{id:guid}/employment-info")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> UpdateEmploymentInfo(Guid id, EmployeeEmploymentInfoRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.UpdateEmploymentInfoAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response, "Employment information updated successfully"));
    }

    [HttpPut("{id:guid}/bank-info")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> UpdateBankInfo(Guid id, EmployeeBankInfoRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.UpdateBankInfoAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response, "Bank information updated successfully"));
    }

    [HttpPut("{id:guid}/next-of-kin")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailsDto>>> UpdateNextOfKin(Guid id, EmployeeNextOfKinRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.UpdateNextOfKinAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDetailsDto>.Ok(response, "Next of kin updated successfully"));
    }

    [HttpPut("{id:guid}/emergency-contact")]
    [HasPermission("Employee.Edit")]
    public async Task<ActionResult<ApiResponse<EmployeeEmergencyContactDto>>> UpsertEmergencyContact(Guid id, EmployeeEmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var response = await employeeService.UpsertEmergencyContactAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeEmergencyContactDto>.Ok(response, "Emergency contact updated successfully"));
    }

    [HttpGet("{id:guid}/job-history")]
    [HasPermission("Employee.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeJobHistoryDto>>>> GetJobHistory(Guid id, CancellationToken cancellationToken)
    {
        var response = await employeeService.GetJobHistoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<EmployeeJobHistoryDto>>.Ok(response));
    }

    [HttpPost("{employeeId:guid}/documents")]
    [HasPermission("EmployeeDocument.Manage")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<EmployeeDocumentDto>>> UploadDocument(Guid employeeId, [FromForm] EmployeeDocumentUploadForm form, CancellationToken cancellationToken)
    {
        var request = new UploadEmployeeDocumentRequest(form.DocumentTypeId, form.Title, form.IssueDate, form.ExpiryDate, form.ConfidentialFlag);
        await using var stream = form.File.OpenReadStream();
        var response = await documentService.UploadAsync(employeeId, request, stream, form.File.FileName, form.File.ContentType, form.File.Length, cancellationToken);
        return Ok(ApiResponse<EmployeeDocumentDto>.Ok(response, "Document uploaded successfully"));
    }

    [HttpGet("{employeeId:guid}/documents")]
    [HasPermission("EmployeeDocument.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeDocumentDto>>>> ListDocuments(Guid employeeId, CancellationToken cancellationToken)
    {
        var response = await documentService.ListEmployeeDocumentsAsync(employeeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<EmployeeDocumentDto>>.Ok(response));
    }
}

public sealed class EmployeeDocumentUploadForm
{
    public Guid DocumentTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool ConfidentialFlag { get; set; }
    public IFormFile File { get; set; } = default!;
}
