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
public sealed class OrganizationController(IOrganizationService organizationService) : ControllerBase
{
    [HttpPut("managers")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<ManagerAssignmentDto>>> AssignManager(AssignManagerRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.AssignManagerAsync(request, cancellationToken);
        return Ok(ApiResponse<ManagerAssignmentDto>.Ok(response, "Manager assignment updated successfully"));
    }
}
