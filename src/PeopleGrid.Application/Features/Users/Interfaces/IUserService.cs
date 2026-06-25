using PeopleGrid.Application.Features.Users.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Users.Interfaces;

public interface IUserService
{
    Task<UserDetailsDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<AdminResetPasswordResponse> ResetPasswordAsync(Guid id, AdminResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default);
}
