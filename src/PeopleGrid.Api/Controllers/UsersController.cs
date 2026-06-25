using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Users.DTOs;
using PeopleGrid.Application.Features.Users.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [HasPermission("User.Create")]
    public async Task<ActionResult<ApiResponse<UserDetailsDto>>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<UserDetailsDto>.Ok(response, "User created successfully"));
    }

    [HttpGet]
    [HasPermission("User.View")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserListItemDto>>>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        var response = await userService.ListAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<UserListItemDto>>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("User.View")]
    public async Task<ActionResult<ApiResponse<UserDetailsDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await userService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDetailsDto>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("User.Edit")]
    public async Task<ActionResult<ApiResponse<UserDetailsDto>>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserDetailsDto>.Ok(response, "User updated successfully"));
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission("User.Edit")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(Guid id, CancellationToken cancellationToken)
    {
        await userService.ActivateAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "User activated successfully"));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission("User.Deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeactivateAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "User deactivated successfully"));
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission("User.Edit")]
    public async Task<ActionResult<ApiResponse<UserDetailsDto>>> AssignRoles(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.AssignRolesAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserDetailsDto>.Ok(response, "User roles updated successfully"));
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission("User.Edit")]
    public async Task<ActionResult<ApiResponse<AdminResetPasswordResponse>>> ResetPassword(Guid id, AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ResetPasswordAsync(id, request, cancellationToken);
        return Ok(ApiResponse<AdminResetPasswordResponse>.Ok(response, "User password reset successfully"));
    }

    [HttpGet("lookups")]
    [HasPermission("User.View")]
    public async Task<ActionResult<ApiResponse<UserLookupsDto>>> Lookups(CancellationToken cancellationToken)
    {
        var response = await userService.GetLookupsAsync(cancellationToken);
        return Ok(ApiResponse<UserLookupsDto>.Ok(response));
    }
}
