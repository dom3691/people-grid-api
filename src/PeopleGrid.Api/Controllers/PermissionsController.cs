using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Roles.DTOs;
using PeopleGrid.Application.Features.Roles.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PermissionsController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Permission.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionModuleDto>>>> Get(CancellationToken cancellationToken)
    {
        var response = await roleService.GetPermissionCatalogAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PermissionModuleDto>>.Ok(response));
    }
}
