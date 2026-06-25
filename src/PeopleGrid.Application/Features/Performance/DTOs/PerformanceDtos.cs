namespace PeopleGrid.Application.Features.Performance.DTOs;

public sealed record PerformanceCycleRequest(string Name, DateOnly StartDate, DateOnly EndDate, string Status = "Draft", bool IsActive = false);
public sealed record PerformanceCycleDto(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, string Status, bool IsActive);
public sealed record EmployeeGoalRequest(Guid EmployeeId, Guid CycleId, string Title, string? Description, string Target, decimal Weight);
public sealed record EmployeeGoalDto(Guid Id, Guid EmployeeId, Guid CycleId, string Title, string Target, decimal Weight, string Status);
public sealed record AssessmentRequest(Guid EmployeeId, Guid CycleId, Guid? GoalId, Guid? KpiId, string Rating, string? Comments);
public sealed record HrReviewRequest(Guid EmployeeId, Guid CycleId, string ReviewComments);
public sealed record ReleaseRatingRequest(string FinalRating);
public sealed record PromotionRecommendationRequest(Guid EmployeeId, Guid CycleId, string Recommendation, string Justification);
public sealed record PerformanceImprovementPlanRequest(Guid EmployeeId, Guid CycleId, string Objective, DateOnly StartDate, DateOnly EndDate);
public sealed record PerformanceHistoryDto(Guid EmployeeId, Guid CycleId, string FinalRating, bool PromotionRecommended, bool PipTriggered, DateTime CompletedAt);

