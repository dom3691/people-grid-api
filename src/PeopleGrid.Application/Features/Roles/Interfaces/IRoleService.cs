using PeopleGrid.Application.Features.Roles.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Roles.Interfaces;

public interface IRoleService
{
    Task<RoleDetailsDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<RoleListItemDto>> ListAsync(RoleListQuery query, CancellationToken cancellationToken = default);
    Task<RoleDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleDetailsDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionModuleDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PermissionModuleDto>> GetRolePermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleDetailsDto> AssignPermissionsAsync(Guid id, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleUserDto>> GetRoleUsersAsync(Guid id, CancellationToken cancellationToken = default);
}
