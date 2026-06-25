using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Organization.DTOs;

public sealed record CreateDepartmentRequest(string Code, string Name, Guid? HeadUserId, string Status);
public sealed record UpdateDepartmentRequest(string Code, string Name, Guid? HeadUserId, string Status);
public sealed record UpdateStatusRequest(string Status);

public sealed record CreateUnitRequest(Guid DepartmentId, string Code, string Name, string Status);
public sealed record UpdateUnitRequest(Guid DepartmentId, string Code, string Name, string Status);

public sealed record CreateBranchRequest(string Code, string Name, string? Address, string? Country, string? StateRegion, string Status);
public sealed record UpdateBranchRequest(string Code, string Name, string? Address, string? Country, string? StateRegion, string Status);

public sealed record CreateJobTitleRequest(string Code, string Name, Guid? GradeLevelId, string Status);
public sealed record UpdateJobTitleRequest(string Code, string Name, Guid? GradeLevelId, string Status);

public sealed record AssignManagerRequest(Guid UserId, Guid ManagerUserId, DateTime EffectiveFrom);

public sealed record OrganizationListQuery(
    string? Search,
    string? Status,
    string? SortBy,
    string? SortDirection,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record UnitListQuery(
    string? Search,
    string? Status,
    Guid? DepartmentId,
    string? SortBy,
    string? SortDirection,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    Guid? HeadUserId,
    string? HeadUserName,
    string Status,
    bool IsActive,
    int UnitCount,
    int ActiveUserCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record UnitDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Code,
    string Name,
    string Status,
    bool IsActive,
    int ActiveUserCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record BranchDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string? Country,
    string? StateRegion,
    string Status,
    bool IsActive,
    int ActiveUserCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record JobTitleDto(
    Guid Id,
    string Code,
    string Name,
    Guid? GradeLevelId,
    string? GradeLevelName,
    string Status,
    bool IsActive,
    int ActiveUserCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ManagerAssignmentDto(
    Guid Id,
    Guid UserId,
    string UserName,
    Guid ManagerUserId,
    string ManagerName,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsCurrent);
