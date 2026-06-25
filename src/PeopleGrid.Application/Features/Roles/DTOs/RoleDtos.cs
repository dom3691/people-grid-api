using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Roles.DTOs;

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string? Description,
    string Status,
    IReadOnlyCollection<Guid>? PermissionIds);

public sealed record UpdateRoleRequest(
    string Code,
    string Name,
    string? Description,
    string Status);

public sealed record AssignRolePermissionsRequest(IReadOnlyCollection<Guid> PermissionIds);

public sealed record RoleListQuery(
    string? Search,
    string? Status,
    bool? IsSystemRole,
    string? SortBy,
    string? SortDirection,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record RoleListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystemRole,
    string Status,
    bool IsActive,
    int UserCount,
    int PermissionCount,
    DateTime CreatedAt);

public sealed record RoleDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystemRole,
    string Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<PermissionDto> Permissions,
    int UserCount);

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Module,
    string Action,
    string? Description);

public sealed record PermissionModuleDto(
    string Module,
    IReadOnlyCollection<PermissionDto> Permissions);

public sealed record RoleUserDto(
    Guid Id,
    string EmployeeNumber,
    string Email,
    string UserName,
    string FullName,
    string Status,
    DateTime AssignedAt,
    string? AssignedBy);
