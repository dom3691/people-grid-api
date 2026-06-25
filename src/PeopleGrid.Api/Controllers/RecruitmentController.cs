using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Recruitment.DTOs;
using PeopleGrid.Application.Features.Recruitment.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/recruitment")]
[Authorize]
public sealed class RecruitmentController(IRecruitmentService recruitmentService) : ControllerBase
{
    [HttpPost("job-openings")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<JobOpeningDto>>> CreateJob(JobOpeningRequest request, CancellationToken ct) => Ok(ApiResponse<JobOpeningDto>.Ok(await recruitmentService.CreateJobOpeningAsync(request, ct)));
    [HttpGet("job-openings")][HasPermission("Recruitment.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<JobOpeningDto>>>> Jobs(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<JobOpeningDto>>.Ok(await recruitmentService.ListJobOpeningsAsync(ct)));
    [HttpPut("job-openings/{id:guid}")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<JobOpeningDto>>> UpdateJob(Guid id, JobOpeningRequest request, CancellationToken ct) => Ok(ApiResponse<JobOpeningDto>.Ok(await recruitmentService.UpdateJobOpeningAsync(id, request, ct)));
    [HttpPost("job-openings/{id:guid}/publish")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<JobOpeningDto>>> Publish(Guid id, PublishVacancyRequest request, CancellationToken ct) => Ok(ApiResponse<JobOpeningDto>.Ok(await recruitmentService.PublishVacancyAsync(id, request, ct)));
    [HttpPost("applications")][AllowAnonymous] public async Task<ActionResult<ApiResponse<ApplicationDto>>> Apply(ApplicationRequest request, CancellationToken ct) => Ok(ApiResponse<ApplicationDto>.Ok(await recruitmentService.CreateApplicationAsync(request, ct)));
    [HttpGet("applications")][HasPermission("Recruitment.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ApplicationDto>>>> Applications([FromQuery] string? status, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<ApplicationDto>>.Ok(await recruitmentService.ListApplicationsAsync(status, ct)));
    [HttpGet("applications/{id:guid}")][HasPermission("Recruitment.View")] public async Task<ActionResult<ApiResponse<ApplicationDto>>> Application(Guid id, CancellationToken ct) => Ok(ApiResponse<ApplicationDto>.Ok(await recruitmentService.GetApplicationAsync(id, ct)));
    [HttpPost("applications/{id:guid}/shortlist")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<ApplicationDto>>> Shortlist(Guid id, CancellationToken ct) => Ok(ApiResponse<ApplicationDto>.Ok(await recruitmentService.ShortlistAsync(id, ct)));
    [HttpPost("interviews")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<object>>> Interview(InterviewScheduleRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await recruitmentService.ScheduleInterviewAsync(request, ct)));
    [HttpPost("interviews/{id:guid}/feedback")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<object>>> Feedback(Guid id, InterviewFeedbackRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await recruitmentService.SubmitFeedbackAsync(id, request, ct)));
    [HttpPatch("applications/{id:guid}/status")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<ApplicationDto>>> Status(Guid id, CandidateStatusRequest request, CancellationToken ct) => Ok(ApiResponse<ApplicationDto>.Ok(await recruitmentService.UpdateStatusAsync(id, request, ct)));
    [HttpPost("applications/{id:guid}/offer-letter")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<object>>> Offer(Guid id, OfferLetterRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await recruitmentService.GenerateOfferLetterAsync(id, request, ct)));
    [HttpPost("applications/{id:guid}/convert-to-employee")][HasPermission("Recruitment.Manage")] public async Task<ActionResult<ApiResponse<object>>> Convert(Guid id, ConvertCandidateRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await recruitmentService.ConvertToEmployeeAsync(id, request, ct)));
}

