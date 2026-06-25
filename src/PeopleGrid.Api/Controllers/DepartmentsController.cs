using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Organization.DTOs;
using PeopleGrid.Application.Features.Organization.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DepartmentsController(IOrganizationService organizationService) : ControllerBase
{
    [HttpPost]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.CreateDepartmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<DepartmentDto>.Ok(response, "Department created successfully"));
    }

    [HttpGet]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DepartmentDto>>>> List([FromQuery] OrganizationListQuery query, CancellationToken cancellationToken)
    {
        var response = await organizationService.ListDepartmentsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<DepartmentDto>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await organizationService.GetDepartmentAsync(id, cancellationToken);
        return Ok(ApiResponse<DepartmentDto>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.UpdateDepartmentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DepartmentDto>.Ok(response, "Department updated successfully"));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateStatus(Guid id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.UpdateDepartmentStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DepartmentDto>.Ok(response, "Department status updated successfully"));
    }
}
