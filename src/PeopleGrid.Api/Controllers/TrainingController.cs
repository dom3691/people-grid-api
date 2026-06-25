using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Training.DTOs;
using PeopleGrid.Application.Features.Training.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/training")]
[Authorize]
public sealed class TrainingController(ITrainingService trainingService) : ControllerBase
{
    [HttpPost("programs")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> Create(TrainingProgramRequest request, CancellationToken ct) => Ok(ApiResponse<TrainingProgramDto>.Ok(await trainingService.CreateProgramAsync(request, ct)));
    [HttpGet("programs")][HasPermission("Training.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TrainingProgramDto>>>> Programs(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<TrainingProgramDto>>.Ok(await trainingService.ListProgramsAsync(ct)));
    [HttpGet("programs/{id:guid}")][HasPermission("Training.View")] public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> Program(Guid id, CancellationToken ct) => Ok(ApiResponse<TrainingProgramDto>.Ok(await trainingService.GetProgramAsync(id, ct)));
    [HttpPut("programs/{id:guid}")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> Update(Guid id, TrainingProgramRequest request, CancellationToken ct) => Ok(ApiResponse<TrainingProgramDto>.Ok(await trainingService.UpdateProgramAsync(id, request, ct)));
    [HttpPost("programs/{id:guid}/nominations")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<object>>>> Nominate(Guid id, TrainingNominationRequest request, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<object>>.Ok(await trainingService.NominateEmployeesAsync(id, request, ct)));
    [HttpPost("nominations/{id:guid}/approve")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<object>>> Approve(Guid id, TrainingDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await trainingService.ApproveNominationAsync(id, request, ct)));
    [HttpPost("nominations/{id:guid}/reject")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, TrainingDecisionRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await trainingService.RejectNominationAsync(id, request, ct)));
    [HttpPost("programs/{id:guid}/attendance")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<object>>> Attendance(Guid id, TrainingAttendanceRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await trainingService.RecordAttendanceAsync(id, request, ct)));
    [HttpPost("programs/{id:guid}/feedback")][HasPermission("Training.View")] public async Task<ActionResult<ApiResponse<object>>> Feedback(Guid id, TrainingFeedbackRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await trainingService.SubmitFeedbackAsync(id, request, ct)));
    [HttpPost("programs/{id:guid}/certificates")][HasPermission("Training.Manage")] public async Task<ActionResult<ApiResponse<object>>> Certificate(Guid id, TrainingCertificateRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await trainingService.UploadCertificateAsync(id, request, ct)));
    [HttpGet("history/{employeeId:guid}")][HasPermission("Training.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TrainingHistoryDto>>>> History(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<TrainingHistoryDto>>.Ok(await trainingService.GetHistoryAsync(employeeId, ct)));
    [HttpGet("skills/{employeeId:guid}")][HasPermission("Training.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeSkillDto>>>> Skills(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<EmployeeSkillDto>>.Ok(await trainingService.GetSkillsAsync(employeeId, ct)));
}

