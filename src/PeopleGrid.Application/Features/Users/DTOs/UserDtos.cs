using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Users.DTOs;

public sealed record CreateUserRequest(
    string EmployeeNumber,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string? Phone,
    Guid? DepartmentId,
    Guid? UnitId,
    Guid? BranchId,
    Guid? JobTitleId,
    Guid? EmploymentTypeId,
    IReadOnlyCollection<Guid> RoleIds,
    string? Password);

public sealed record UpdateUserRequest(
    string EmployeeNumber,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string? Phone,
    Guid? DepartmentId,
    Guid? UnitId,
    Guid? BranchId,
    Guid? JobTitleId,
    Guid? EmploymentTypeId,
    string Status);

public sealed record AssignUserRolesRequest(IReadOnlyCollection<Guid> RoleIds);

public sealed record AdminResetPasswordRequest(string? NewPassword);

public sealed record AdminResetPasswordResponse(string TemporaryPassword);

public sealed record UserListQuery(
    string? Search,
    string? Status,
    Guid? DepartmentId,
    Guid? BranchId,
    Guid? RoleId,
    Guid? EmploymentTypeId,
    string? SortBy,
    string? SortDirection,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record UserListItemDto(
    Guid Id,
    string EmployeeNumber,
    string Email,
    string UserName,
    string FullName,
    string Status,
    string? Department,
    string? Branch,
    string? EmploymentType,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public sealed record UserDetailsDto(
    Guid Id,
    string EmployeeNumber,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string? Phone,
    string Status,
    bool IsActive,
    DateTime? LockoutEnd,
    int FailedLoginCount,
    DateTime? LastLoginAt,
    Guid? DepartmentId,
    string? Department,
    Guid? UnitId,
    string? Unit,
    Guid? BranchId,
    string? Branch,
    Guid? JobTitleId,
    string? JobTitle,
    Guid? EmploymentTypeId,
    string? EmploymentType,
    IReadOnlyCollection<RoleSummaryDto> Roles);

public sealed record RoleSummaryDto(Guid Id, string Name, string Code);

public sealed record LookupItemDto(Guid Id, string Code, string Name);

public sealed record UserLookupsDto(
    IReadOnlyCollection<LookupItemDto> Departments,
    IReadOnlyCollection<LookupItemDto> Units,
    IReadOnlyCollection<LookupItemDto> Branches,
    IReadOnlyCollection<LookupItemDto> JobTitles,
    IReadOnlyCollection<LookupItemDto> EmploymentTypes,
    IReadOnlyCollection<RoleSummaryDto> Roles);
