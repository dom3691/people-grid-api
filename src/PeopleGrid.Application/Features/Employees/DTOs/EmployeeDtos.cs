using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.Employees.DTOs;

public sealed record EmployeeListQuery(
    string? Search,
    string? Status,
    Guid? DepartmentId,
    Guid? BranchId,
    Guid? JobTitleId,
    Guid? GradeLevelId,
    Guid? LineManagerId,
    string? SortBy,
    string? SortDirection,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record CreateEmployeeRequest(
    string? EmployeeNumber,
    Guid? UserId,
    EmployeePersonalInfoRequest PersonalInfo,
    EmployeeContactInfoRequest ContactInfo,
    EmployeeEmploymentInfoRequest EmploymentInfo,
    string Status = "Active");

public sealed record UpdateEmployeeRequest(
    EmployeePersonalInfoRequest PersonalInfo,
    EmployeeContactInfoRequest ContactInfo,
    EmployeeEmploymentInfoRequest EmploymentInfo,
    string Status);

public sealed record EmployeePersonalInfoRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? MaritalStatus,
    string? Nationality);

public sealed record EmployeeContactInfoRequest(
    string WorkEmail,
    string? PersonalEmail,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? Country);

public sealed record EmployeeEmploymentInfoRequest(
    Guid? DepartmentId,
    Guid? UnitId,
    Guid? BranchId,
    Guid? JobTitleId,
    Guid? GradeLevelId,
    Guid? EmploymentTypeId,
    Guid? LineManagerId,
    DateOnly HireDate,
    DateOnly? ConfirmationDate);

public sealed record EmployeeBankInfoRequest(
    string BankName,
    string? BankCode,
    string AccountNumber,
    string AccountName);

public sealed record EmployeeNextOfKinRequest(
    string Name,
    string Relationship,
    string Phone,
    string? Email,
    string? Address);

public sealed record EmployeeEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? Email,
    string? Address,
    int Priority);

public sealed record ChangeEmployeeStatusRequest(string Status, string? Reason, DateOnly? EffectiveDate);

public sealed record DeactivateEmployeeRequest(string Reason);

public sealed record GenerateEmployeeNumberRequest(string? Prefix);

public sealed record GenerateEmployeeNumberResponse(string EmployeeNumber);

public sealed record EmployeeListItemDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string WorkEmail,
    string Status,
    string? Department,
    string? Branch,
    string? JobTitle,
    string? GradeLevel,
    string? LineManager,
    DateTime CreatedAt);

public sealed record EmployeeDetailsDto(
    Guid Id,
    string EmployeeNumber,
    Guid? UserId,
    string Status,
    DateTime? DeactivatedAt,
    string? DeactivationReason,
    EmployeePersonalInfoDto? PersonalInfo,
    EmployeeContactInfoDto? ContactInfo,
    EmployeeEmploymentInfoDto? EmploymentInfo,
    EmployeeBankInfoDto? BankInfo,
    EmployeeNextOfKinDto? NextOfKin,
    IReadOnlyCollection<EmployeeEmergencyContactDto> EmergencyContacts);

public sealed record EmployeePersonalInfoDto(string FirstName, string? MiddleName, string LastName, DateOnly DateOfBirth, string Gender, string? MaritalStatus, string? Nationality);
public sealed record EmployeeContactInfoDto(string WorkEmail, string? PersonalEmail, string? Phone, string? Address, string? City, string? State, string? Country);
public sealed record EmployeeEmploymentInfoDto(Guid? DepartmentId, string? Department, Guid? UnitId, string? Unit, Guid? BranchId, string? Branch, Guid? JobTitleId, string? JobTitle, Guid? GradeLevelId, string? GradeLevel, Guid? EmploymentTypeId, string? EmploymentType, Guid? LineManagerId, string? LineManager, DateOnly HireDate, DateOnly? ConfirmationDate);
public sealed record EmployeeBankInfoDto(string BankName, string? BankCode, string MaskedAccountNumber, string AccountName);
public sealed record EmployeeNextOfKinDto(string Name, string Relationship, string Phone, string? Email, string? Address);
public sealed record EmployeeEmergencyContactDto(Guid Id, string Name, string Relationship, string Phone, string? Email, string? Address, int Priority);
public sealed record EmployeeJobHistoryDto(Guid Id, DateOnly EffectiveDate, Guid? FromJobTitleId, string? FromJobTitle, Guid? ToJobTitleId, string? ToJobTitle, Guid? FromDepartmentId, string? FromDepartment, Guid? ToDepartmentId, string? ToDepartment, string? Reason);
