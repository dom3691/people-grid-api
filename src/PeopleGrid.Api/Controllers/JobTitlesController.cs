using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Organization.DTOs;
using PeopleGrid.Application.Features.Organization.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/job-titles")]
[Authorize]
public sealed class JobTitlesController(IOrganizationService organizationService) : ControllerBase
{
    [HttpPost]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Create(CreateJobTitleRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.CreateJobTitleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = response.Id }, ApiResponse<JobTitleDto>.Ok(response, "Job title created successfully"));
    }

    [HttpGet]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<JobTitleDto>>>> List([FromQuery] OrganizationListQuery query, CancellationToken cancellationToken)
    {
        var response = await organizationService.ListJobTitlesAsync(query, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<JobTitleDto>>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Department.Manage")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Update(Guid id, UpdateJobTitleRequest request, CancellationToken cancellationToken)
    {
        var response = await organizationService.UpdateJobTitleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<JobTitleDto>.Ok(response, "Job title updated successfully"));
    }
}
