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
public sealed class UnitsController(IOrganizationService organizationService) : ControllerBase
{
    [HttpPost]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<UnitDto>>> Create(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.CreateUnitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = response.Id }, ApiResponse<UnitDto>.Ok(response, "Unit created successfully"));
    }

    [HttpGet]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UnitDto>>>> List([FromQuery] UnitListQuery query, CancellationToken cancellationToken)
    {
        var response = await organizationService.ListUnitsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<UnitDto>>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<UnitDto>>> Update(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.UpdateUnitAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UnitDto>.Ok(response, "Unit updated successfully"));
    }
}
