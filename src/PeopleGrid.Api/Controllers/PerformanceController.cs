using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Performance.DTOs;
using PeopleGrid.Application.Features.Performance.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/performance")]
[Authorize]
public sealed class PerformanceController(IPerformanceService performanceService) : ControllerBase
{
    [HttpPost("cycles")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<PerformanceCycleDto>>> CreateCycle(PerformanceCycleRequest request, CancellationToken ct) => Ok(ApiResponse<PerformanceCycleDto>.Ok(await performanceService.CreateCycleAsync(request, ct)));
    [HttpGet("cycles")][HasPermission("Performance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PerformanceCycleDto>>>> Cycles(CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PerformanceCycleDto>>.Ok(await performanceService.ListCyclesAsync(ct)));
    [HttpPut("cycles/{id:guid}")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<PerformanceCycleDto>>> UpdateCycle(Guid id, PerformanceCycleRequest request, CancellationToken ct) => Ok(ApiResponse<PerformanceCycleDto>.Ok(await performanceService.UpdateCycleAsync(id, request, ct)));
    [HttpPost("goals")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<EmployeeGoalDto>>> CreateGoal(EmployeeGoalRequest request, CancellationToken ct) => Ok(ApiResponse<EmployeeGoalDto>.Ok(await performanceService.CreateGoalAsync(request, ct)));
    [HttpGet("goals/{employeeId:guid}")][HasPermission("Performance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EmployeeGoalDto>>>> Goals(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<EmployeeGoalDto>>.Ok(await performanceService.GetEmployeeGoalsAsync(employeeId, ct)));
    [HttpPut("goals/{id:guid}")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<EmployeeGoalDto>>> UpdateGoal(Guid id, EmployeeGoalRequest request, CancellationToken ct) => Ok(ApiResponse<EmployeeGoalDto>.Ok(await performanceService.UpdateGoalAsync(id, request, ct)));
    [HttpPost("self-assessments")][HasPermission("Performance.View")] public async Task<ActionResult<ApiResponse<object>>> SelfAssessment(AssessmentRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.SubmitSelfAssessmentAsync(request, ct)));
    [HttpPost("manager-assessments")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<object>>> ManagerAssessment(AssessmentRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.SubmitManagerAssessmentAsync(request, ct)));
    [HttpPost("hr-reviews")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<object>>> HrReview(HrReviewRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.SubmitHrReviewAsync(request, ct)));
    [HttpPost("ratings/{id:guid}/release")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<object>>> Release(Guid id, ReleaseRatingRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.ReleaseRatingAsync(id, request, ct)));
    [HttpPost("promotion-recommendations")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<object>>> Promotion(PromotionRecommendationRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.SubmitPromotionRecommendationAsync(request, ct)));
    [HttpPost("pips")][HasPermission("Performance.Manage")] public async Task<ActionResult<ApiResponse<object>>> Pip(PerformanceImprovementPlanRequest request, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await performanceService.CreatePipAsync(request, ct)));
    [HttpGet("history/{employeeId:guid}")][HasPermission("Performance.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PerformanceHistoryDto>>>> History(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<PerformanceHistoryDto>>.Ok(await performanceService.GetHistoryAsync(employeeId, ct)));
}

