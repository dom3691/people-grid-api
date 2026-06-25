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
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpPost]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<RoleDetailsDto>.Ok(response, "Role created successfully"));
    }

    [HttpGet]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<RoleListItemDto>>>> List([FromQuery] RoleListQuery query, CancellationToken cancellationToken)
    {
        var response = await roleService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<RoleListItemDto>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await roleService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<RoleDetailsDto>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await roleService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RoleDetailsDto>.Ok(response, "Role updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Role deleted successfully"));
    }

    [HttpGet("{id:guid}/permissions")]
    [HasPermission("Role.Manage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionModuleDto>>>> GetPermissions(Guid id, CancellationToken cancellationToken)
    {
        var response = await roleService.GetRolePermissionsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PermissionModuleDto>>.Ok(response));
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission("Permission.Manage")]
    public async Task<ActionResult<ApiResponse<RoleDetailsDto>>> AssignPermissions(Guid id, AssignRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var response = await roleService.AssignPermissionsAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RoleDetailsDto>.Ok(response, "Role permissions updated successfully"));
    }

    [HttpGet("{id:guid}/users")]
    [HasPermission("User.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleUserDto>>>> GetUsers(Guid id, CancellationToken cancellationToken)
    {
        var response = await roleService.GetRoleUsersAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<RoleUserDto>>.Ok(response));
    }
}
