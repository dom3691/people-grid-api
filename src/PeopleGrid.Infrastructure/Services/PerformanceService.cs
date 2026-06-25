using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Performance.DTOs;
using PeopleGrid.Application.Features.Performance.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public sealed class PerformanceService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IPerformanceService
{
    private static readonly HashSet<string> Ratings = ["Excellent", "Very Good", "Good", "Average", "Poor"];

    public async Task<PerformanceCycleDto> CreateCycleAsync(PerformanceCycleRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateCycle(request);
        var entity = new PerformanceCycle { Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate, Status = request.Status, IsActive = request.IsActive };
        dbContext.PerformanceCycles.Add(entity); AddAudit("CreateCycle", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<PerformanceCycleDto>> ListCyclesAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.PerformanceCycles.AsNoTracking().OrderByDescending(x => x.StartDate).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<PerformanceCycleDto> UpdateCycleAsync(Guid id, PerformanceCycleRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateCycle(request);
        var entity = await dbContext.PerformanceCycles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Performance cycle was not found.");
        entity.Name = request.Name.Trim(); entity.StartDate = request.StartDate; entity.EndDate = request.EndDate; entity.Status = request.Status; entity.IsActive = request.IsActive;
        AddAudit("UpdateCycle", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<EmployeeGoalDto> CreateGoalAsync(EmployeeGoalRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); await ValidateEmployeeCycleAsync(request.EmployeeId, request.CycleId, cancellationToken); await ValidateWeightAsync(request.EmployeeId, request.CycleId, request.Weight, null, cancellationToken);
        var entity = new EmployeeGoal { EmployeeId = request.EmployeeId, CycleId = request.CycleId, Title = request.Title.Trim(), Description = request.Description, Target = request.Target.Trim(), Weight = request.Weight, Status = "Active" };
        dbContext.EmployeeGoals.Add(entity); AddAudit("CreateGoal", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<EmployeeGoalDto>> GetEmployeeGoalsAsync(Guid employeeId, CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.EmployeeGoals.AsNoTracking().Where(x => x.EmployeeId == employeeId).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<EmployeeGoalDto> UpdateGoalAsync(Guid id, EmployeeGoalRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await dbContext.EmployeeGoals.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Employee goal was not found.");
        await ValidateWeightAsync(request.EmployeeId, request.CycleId, request.Weight, id, cancellationToken);
        entity.EmployeeId = request.EmployeeId; entity.CycleId = request.CycleId; entity.Title = request.Title.Trim(); entity.Description = request.Description; entity.Target = request.Target.Trim(); entity.Weight = request.Weight;
        AddAudit("UpdateGoal", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<object> SubmitSelfAssessmentAsync(AssessmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRating(request.Rating); await ValidateEmployeeCycleAsync(request.EmployeeId, request.CycleId, cancellationToken);
        var entity = new SelfAssessment { EmployeeId = request.EmployeeId, CycleId = request.CycleId, GoalId = request.GoalId, KpiId = request.KpiId, SelfRating = request.Rating, Comments = request.Comments };
        dbContext.SelfAssessments.Add(entity); AddAudit("SelfAssessment", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return new { entity.Id, entity.SelfRating, entity.SubmittedAt };
    }

    public async Task<object> SubmitManagerAssessmentAsync(AssessmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateRating(request.Rating);
        if (!await dbContext.SelfAssessments.AnyAsync(x => x.EmployeeId == request.EmployeeId && x.CycleId == request.CycleId, cancellationToken)) throw new BusinessRuleException("Self-assessment must be submitted first.");
        var entity = new ManagerAssessment { EmployeeId = request.EmployeeId, CycleId = request.CycleId, GoalId = request.GoalId, KpiId = request.KpiId, ManagerRating = request.Rating, Comments = request.Comments, SubmittedBy = CurrentUserGuid() };
        dbContext.ManagerAssessments.Add(entity); AddAudit("ManagerAssessment", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return new { entity.Id, entity.ManagerRating, entity.SubmittedAt };
    }

    public async Task<object> SubmitHrReviewAsync(HrReviewRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = new HrPerformanceReview { EmployeeId = request.EmployeeId, CycleId = request.CycleId, ReviewComments = request.ReviewComments.Trim(), ReviewedBy = CurrentUserGuid() };
        dbContext.HrPerformanceReviews.Add(entity); AddAudit("HrReview", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return new { entity.Id, entity.ReviewedAt };
    }

    public async Task<object> ReleaseRatingAsync(Guid id, ReleaseRatingRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateRating(request.FinalRating);
        var rating = await dbContext.PerformanceRatings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rating is null) throw new NotFoundException("Performance rating was not found.");
        if (rating.IsReleased) throw new BusinessRuleException("Released appraisal cannot be edited without authorized reopen.");
        rating.FinalRating = request.FinalRating; rating.IsReleased = true; rating.ReleasedBy = CurrentUserGuid(); rating.ReleasedAt = DateTime.UtcNow;
        dbContext.PerformanceHistories.Add(new PerformanceHistory { EmployeeId = rating.EmployeeId, CycleId = rating.CycleId, FinalRating = rating.FinalRating, PromotionRecommended = await dbContext.PromotionRecommendations.AnyAsync(x => x.EmployeeId == rating.EmployeeId && x.CycleId == rating.CycleId, cancellationToken), PipTriggered = rating.FinalRating == "Poor" });
        AddAudit("ReleaseRating", id); await dbContext.SaveChangesAsync(cancellationToken); return new { rating.Id, rating.FinalRating, rating.ReleasedAt };
    }

    public async Task<object> SubmitPromotionRecommendationAsync(PromotionRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = new PromotionRecommendation { EmployeeId = request.EmployeeId, CycleId = request.CycleId, Recommendation = request.Recommendation.Trim(), Justification = request.Justification.Trim(), RecommendedBy = CurrentUserGuid() };
        dbContext.PromotionRecommendations.Add(entity); AddAudit("PromotionRecommendation", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return new { entity.Id, entity.Status };
    }

    public async Task<object> CreatePipAsync(PerformanceImprovementPlanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (request.EndDate <= request.StartDate) throw new BusinessRuleException("PIP end date must be after start date.");
        var entity = new PerformanceImprovementPlan { EmployeeId = request.EmployeeId, CycleId = request.CycleId, Objective = request.Objective.Trim(), StartDate = request.StartDate, EndDate = request.EndDate };
        dbContext.PerformanceImprovementPlans.Add(entity); AddAudit("CreatePip", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return new { entity.Id, entity.Status };
    }

    public async Task<IReadOnlyCollection<PerformanceHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await dbContext.PerformanceHistories.AsNoTracking().Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.CompletedAt).Select(x => new PerformanceHistoryDto(x.EmployeeId, x.CycleId, x.FinalRating, x.PromotionRecommended, x.PipTriggered, x.CompletedAt)).ToListAsync(cancellationToken);
    }

    private async Task ValidateEmployeeCycleAsync(Guid employeeId, Guid cycleId, CancellationToken ct)
    {
        if (!await dbContext.Employees.AnyAsync(x => x.Id == employeeId, ct)) throw new BusinessRuleException("Employee is invalid.");
        if (!await dbContext.PerformanceCycles.AnyAsync(x => x.Id == cycleId, ct)) throw new BusinessRuleException("Performance cycle is invalid.");
    }

    private async Task ValidateWeightAsync(Guid employeeId, Guid cycleId, decimal weight, Guid? excludeGoalId, CancellationToken ct)
    {
        if (weight <= 0) throw new BusinessRuleException("Weight must be positive.");
        var total = await dbContext.EmployeeGoals.Where(x => x.EmployeeId == employeeId && x.CycleId == cycleId && x.Id != excludeGoalId).SumAsync(x => x.Weight, ct) + weight;
        if (total > 100) throw new BusinessRuleException("Goal weights cannot exceed 100%.");
    }

    private static void ValidateCycle(PerformanceCycleRequest request) { if (request.EndDate <= request.StartDate) throw new BusinessRuleException("Cycle end date must be after start date."); }
    private static void ValidateRating(string rating) { if (!Ratings.Contains(rating)) throw new BusinessRuleException("Rating must be Excellent, Very Good, Good, Average, or Poor."); }
    private static PerformanceCycleDto Map(PerformanceCycle x) => new(x.Id, x.Name, x.StartDate, x.EndDate, x.Status, x.IsActive);
    private static EmployeeGoalDto Map(EmployeeGoal x) => new(x.Id, x.EmployeeId, x.CycleId, x.Title, x.Target, x.Weight, x.Status);
    private void AddAudit(string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = "Performance", Action = action, EntityType = "Performance", EntityId = id.ToString(), Outcome = "Success" });
    private void EnsureView() { if (!currentUser.Permissions.Contains("Performance.View") && !currentUser.Permissions.Contains("Performance.Manage")) throw new ForbiddenException("Performance view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Performance.Manage")) throw new ForbiddenException("Performance management permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}

