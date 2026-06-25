using PeopleGrid.Application.Features.Settings.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Settings.Interfaces;

public interface ISettingsService
{
    Task<CompanyProfileDto?> GetCompanyProfileAsync(CancellationToken cancellationToken = default);
    Task<CompanyProfileDto> UpdateCompanyProfileAsync(CompanyProfileRequest request, CancellationToken cancellationToken = default);

    Task<GradeLevelDto> CreateGradeLevelAsync(GradeLevelRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<GradeLevelDto>> ListGradeLevelsAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<GradeLevelDto> UpdateGradeLevelAsync(Guid id, GradeLevelRequest request, CancellationToken cancellationToken = default);

    Task<EmploymentTypeDto> CreateEmploymentTypeAsync(EmploymentTypeRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<EmploymentTypeDto>> ListEmploymentTypesAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<EmploymentTypeDto> UpdateEmploymentTypeAsync(Guid id, EmploymentTypeRequest request, CancellationToken cancellationToken = default);

    Task<ApprovalLevelDto> CreateApprovalLevelAsync(ApprovalLevelRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<ApprovalLevelDto>> ListApprovalLevelsAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<ApprovalLevelDto> UpdateApprovalLevelAsync(Guid id, ApprovalLevelRequest request, CancellationToken cancellationToken = default);

    Task<LeaveTypeDto> CreateLeaveTypeAsync(LeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<LeaveTypeDto>> ListLeaveTypesAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, LeaveTypeRequest request, CancellationToken cancellationToken = default);

    Task<PublicHolidayDto> CreatePublicHolidayAsync(PublicHolidayRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<PublicHolidayDto>> ListPublicHolidaysAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<PublicHolidayDto> UpdatePublicHolidayAsync(Guid id, PublicHolidayRequest request, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<SystemParameterDto>> ListSystemParametersAsync(SettingsListQuery query, CancellationToken cancellationToken = default);
    Task<SystemParameterDto> UpdateSystemParameterAsync(string key, SystemParameterRequest request, CancellationToken cancellationToken = default);
}
