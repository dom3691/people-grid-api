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
public sealed class BranchesController(IOrganizationService organizationService) : ControllerBase
{
    [HttpPost]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.CreateBranchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = response.Id }, ApiResponse<BranchDto>.Ok(response, "Branch created successfully"));
    }

    [HttpGet]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<BranchDto>>>> List([FromQuery] OrganizationListQuery query, CancellationToken cancellationToken)
    {
        var response = await organizationService.ListBranchesAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<BranchDto>>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.UpdateBranchAsync(id, request, cancellationToken);
        return Ok(ApiResponse<BranchDto>.Ok(response, "Branch updated successfully"));
    }
}
