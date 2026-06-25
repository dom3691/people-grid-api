using PeopleGrid.Application.Features.Performance.DTOs;

namespace PeopleGrid.Application.Features.Performance.Interfaces;

public interface IPerformanceService
{
    Task<PerformanceCycleDto> CreateCycleAsync(PerformanceCycleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PerformanceCycleDto>> ListCyclesAsync(CancellationToken cancellationToken = default);
    Task<PerformanceCycleDto> UpdateCycleAsync(Guid id, PerformanceCycleRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeGoalDto> CreateGoalAsync(EmployeeGoalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeGoalDto>> GetEmployeeGoalsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeGoalDto> UpdateGoalAsync(Guid id, EmployeeGoalRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitSelfAssessmentAsync(AssessmentRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitManagerAssessmentAsync(AssessmentRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitHrReviewAsync(HrReviewRequest request, CancellationToken cancellationToken = default);
    Task<object> ReleaseRatingAsync(Guid id, ReleaseRatingRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitPromotionRecommendationAsync(PromotionRecommendationRequest request, CancellationToken cancellationToken = default);
    Task<object> CreatePipAsync(PerformanceImprovementPlanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PerformanceHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

