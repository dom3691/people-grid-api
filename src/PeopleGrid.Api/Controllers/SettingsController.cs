using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Settings.DTOs;
using PeopleGrid.Application.Features.Settings.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
[HasPermission("Settings.Manage")]
public sealed class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet("company-profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto?>>> GetCompanyProfile(CancellationToken cancellationToken)
    {
        var response = await settingsService.GetCompanyProfileAsync(cancellationToken);
        return Ok(ApiResponse<CompanyProfileDto?>.Ok(response));
    }

    [HttpPut("company-profile")]
    public async Task<ActionResult<ApiResponse<CompanyProfileDto>>> UpdateCompanyProfile(CompanyProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateCompanyProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<CompanyProfileDto>.Ok(response, "Company profile updated successfully"));
    }

    [HttpPost("grade-levels")]
    public async Task<ActionResult<ApiResponse<GradeLevelDto>>> CreateGradeLevel(GradeLevelRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateGradeLevelAsync(request, cancellationToken);
        return Ok(ApiResponse<GradeLevelDto>.Ok(response, "Grade level created successfully"));
    }

    [HttpGet("grade-levels")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<GradeLevelDto>>>> ListGradeLevels([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListGradeLevelsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<GradeLevelDto>>.Ok(response));
    }

    [HttpPut("grade-levels/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GradeLevelDto>>> UpdateGradeLevel(Guid id, GradeLevelRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateGradeLevelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<GradeLevelDto>.Ok(response, "Grade level updated successfully"));
    }

    [HttpPost("employment-types")]
    public async Task<ActionResult<ApiResponse<EmploymentTypeDto>>> CreateEmploymentType(EmploymentTypeRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateEmploymentTypeAsync(request, cancellationToken);
        return Ok(ApiResponse<EmploymentTypeDto>.Ok(response, "Employment type created successfully"));
    }

    [HttpGet("employment-types")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EmploymentTypeDto>>>> ListEmploymentTypes([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListEmploymentTypesAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<EmploymentTypeDto>>.Ok(response));
    }

    [HttpPut("employment-types/{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmploymentTypeDto>>> UpdateEmploymentType(Guid id, EmploymentTypeRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateEmploymentTypeAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmploymentTypeDto>.Ok(response, "Employment type updated successfully"));
    }

    [HttpPost("approval-levels")]
    public async Task<ActionResult<ApiResponse<ApprovalLevelDto>>> CreateApprovalLevel(ApprovalLevelRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateApprovalLevelAsync(request, cancellationToken);
        return Ok(ApiResponse<ApprovalLevelDto>.Ok(response, "Approval level created successfully"));
    }

    [HttpGet("approval-levels")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ApprovalLevelDto>>>> ListApprovalLevels([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListApprovalLevelsAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<ApprovalLevelDto>>.Ok(response));
    }

    [HttpPut("approval-levels/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ApprovalLevelDto>>> UpdateApprovalLevel(Guid id, ApprovalLevelRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateApprovalLevelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ApprovalLevelDto>.Ok(response, "Approval level updated successfully"));
    }

    [HttpPost("leave-types")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> CreateLeaveType(LeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreateLeaveTypeAsync(request, cancellationToken);
        return Ok(ApiResponse<LeaveTypeDto>.Ok(response, "Leave type created successfully"));
    }

    [HttpGet("leave-types")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LeaveTypeDto>>>> ListLeaveTypes([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListLeaveTypesAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<LeaveTypeDto>>.Ok(response));
    }

    [HttpPut("leave-types/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> UpdateLeaveType(Guid id, LeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateLeaveTypeAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LeaveTypeDto>.Ok(response, "Leave type updated successfully"));
    }

    [HttpPost("public-holidays")]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> CreatePublicHoliday(PublicHolidayRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.CreatePublicHolidayAsync(request, cancellationToken);
        return Ok(ApiResponse<PublicHolidayDto>.Ok(response, "Public holiday created successfully"));
    }

    [HttpGet("public-holidays")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PublicHolidayDto>>>> ListPublicHolidays([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListPublicHolidaysAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<PublicHolidayDto>>.Ok(response));
    }

    [HttpPut("public-holidays/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PublicHolidayDto>>> UpdatePublicHoliday(Guid id, PublicHolidayRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdatePublicHolidayAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PublicHolidayDto>.Ok(response, "Public holiday updated successfully"));
    }

    [HttpGet("system-parameters")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SystemParameterDto>>>> ListSystemParameters([FromQuery] SettingsListQuery query, CancellationToken cancellationToken)
    {
        var response = await settingsService.ListSystemParametersAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<SystemParameterDto>>.Ok(response));
    }

    [HttpPut("system-parameters/{key}")]
    public async Task<ActionResult<ApiResponse<SystemParameterDto>>> UpdateSystemParameter(string key, SystemParameterRequest request, CancellationToken cancellationToken)
    {
        var response = await settingsService.UpdateSystemParameterAsync(key, request, cancellationToken);
        return Ok(ApiResponse<SystemParameterDto>.Ok(response, "System parameter updated successfully"));
    }
}
