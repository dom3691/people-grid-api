using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Settings.DTOs;

public sealed record SettingsListQuery(string? Search, string? Status, int PageNumber = 1, int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record CompanyProfileRequest(string Name, string? RegistrationNumber, string? LogoPath, string? Address, string? ContactEmail, string? Phone);
public sealed record CompanyProfileDto(Guid Id, string Name, string? RegistrationNumber, string? LogoPath, string? Address, string? ContactEmail, string? Phone, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record GradeLevelRequest(string Code, string Name, int RankOrder, string Status);
public sealed record GradeLevelDto(Guid Id, string Code, string Name, int RankOrder, string Status, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record EmploymentTypeRequest(string Code, string Name, string Status);
public sealed record EmploymentTypeDto(Guid Id, string Code, string Name, string Status, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record ApprovalLevelRequest(string Name, int SequenceOrder, string? Description, string Status);
public sealed record ApprovalLevelDto(Guid Id, string Name, int SequenceOrder, string? Description, string Status, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record LeaveTypeRequest(string Code, string Name, decimal DefaultDays, bool RequiresApproval, string Status);
public sealed record LeaveTypeDto(Guid Id, string Code, string Name, decimal DefaultDays, bool RequiresApproval, string Status, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record PublicHolidayRequest(string Name, DateOnly HolidayDate, Guid? BranchId, string? LocationScope, string Status);
public sealed record PublicHolidayDto(Guid Id, string Name, DateOnly HolidayDate, Guid? BranchId, string? BranchName, string? LocationScope, string Status, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record SystemParameterRequest(string Value);
public sealed record SystemParameterDto(Guid Id, string Key, string Value, string DataType, string? Description, bool IsSensitive, DateTime CreatedAt, DateTime? UpdatedAt);
