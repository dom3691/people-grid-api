using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Onboarding.DTOs;
using PeopleGrid.Application.Features.Onboarding.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class OnboardingService(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorageService fileStorage, IEmailService emailService) : IOnboardingService
{
    public async Task<OnboardingPlanDto> CreatePlanAsync(CreateOnboardingPlanRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.Include(x => x.ContactInfo).FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Employee was not found.");
        var template = await dbContext.OnboardingTemplates.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == request.TemplateId && x.IsActive, cancellationToken) ?? throw new NotFoundException("Onboarding template was not found.");
        var plan = new OnboardingPlan { EmployeeId = employee.Id, TemplateId = template.Id, StartDate = request.StartDate, Status = "Open" };
        dbContext.OnboardingPlans.Add(plan);
        foreach (var item in template.Items.OrderBy(x => x.Sequence))
        {
            dbContext.OnboardingTasks.Add(new OnboardingTask { PlanId = plan.Id, ChecklistItem = item.ChecklistItem, OwnerType = item.OwnerType, DueDate = request.StartDate.AddDays(item.DefaultDueDays), Status = "Open", IsMandatory = item.IsMandatory });
        }
        if (request.ProbationEndDate is not null)
        {
            dbContext.ProbationRecords.Add(new ProbationRecord { EmployeeId = employee.Id, StartDate = request.StartDate, EndDate = request.ProbationEndDate.Value, ReviewerUserId = request.ReviewerUserId, Status = "Open" });
        }
        AddAudit("Onboarding", "CreatePlan", plan.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPlanAsync(plan.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<OnboardingPlanDto>> ListPlansAsync(OnboardingPlanQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.OnboardingPlans.AsNoTracking().Include(x => x.Tasks).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<OnboardingPlanDto>(rows.Select(MapPlan).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<OnboardingPlanDto> GetPlanAsync(Guid id, CancellationToken cancellationToken = default) => MapPlan(await dbContext.OnboardingPlans.AsNoTracking().Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Onboarding plan was not found."));

    public async Task<OnboardingPlanDto> UpdatePlanAsync(Guid id, UpdateOnboardingPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.OnboardingPlans.Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Onboarding plan was not found.");
        if (request.Status == "Completed" && plan.Tasks.Any(x => x.IsMandatory && x.Status != "Completed")) throw new BusinessRuleException("Mandatory tasks must be complete before plan can be marked complete.");
        plan.Status = request.Status;
        plan.CompletedAt = request.Status == "Completed" ? DateTime.UtcNow : null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPlan(plan);
    }

    public async Task<OnboardingTaskDto> AddTaskAsync(Guid planId, AddOnboardingTaskRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.OnboardingPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken) ?? throw new NotFoundException("Onboarding plan was not found.");
        if (request.DueDate < plan.StartDate) throw new BusinessRuleException("Checklist due date must be on or after onboarding start date.");
        var task = new OnboardingTask { PlanId = planId, ChecklistItem = request.ChecklistItem.Trim(), OwnerType = request.OwnerType.Trim(), OwnerUserId = request.OwnerUserId, DueDate = request.DueDate, IsMandatory = request.IsMandatory, Status = "Open" };
        dbContext.OnboardingTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTask(task);
    }

    public async Task<OnboardingTaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken) ?? throw new NotFoundException("Onboarding task was not found.");
        task.Status = "Completed"; task.CompletedAt = DateTime.UtcNow; task.CompletedBy = CurrentUserGuidOrNull();
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTask(task);
    }

    public async Task<OnboardingTaskDto> ReopenTaskAsync(Guid taskId, ReopenTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await dbContext.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken) ?? throw new NotFoundException("Onboarding task was not found.");
        task.Status = "Open"; task.CompletedAt = null; task.CompletedBy = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTask(task);
    }

    public async Task SubmitDocumentAsync(Guid planId, Guid? documentTypeId, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.OnboardingPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken) ?? throw new NotFoundException("Onboarding plan was not found.");
        var key = await fileStorage.SaveAsync(fileStream, fileName, "application/octet-stream", cancellationToken);
        dbContext.OnboardingDocuments.Add(new OnboardingDocument { PlanId = plan.Id, EmployeeId = plan.EmployeeId, DocumentTypeId = documentTypeId, FileName = Path.GetFileName(fileName), StorageKey = key, UploadedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.OnboardingPlans.Include(x => x.Employee).ThenInclude(x => x!.ContactInfo).FirstOrDefaultAsync(x => x.Id == planId, cancellationToken) ?? throw new NotFoundException("Onboarding plan was not found.");
        var email = plan.Employee?.ContactInfo?.WorkEmail ?? plan.Employee?.WorkEmail;
        if (string.IsNullOrWhiteSpace(email)) throw new BusinessRuleException("Employee has no work email.");
        await emailService.SendAsync(email, "Welcome to PeopleGrid", "Welcome. Your onboarding plan has been created.", cancellationToken);
    }

    public async Task AcknowledgePolicyAsync(Guid planId, AcknowledgePolicyRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.OnboardingPlans.AnyAsync(x => x.Id == planId && x.EmployeeId == request.EmployeeId, cancellationToken)) throw new NotFoundException("Onboarding plan was not found.");
        dbContext.PolicyAcknowledgements.Add(new PolicyAcknowledgement { EmployeeId = request.EmployeeId, PolicyId = request.PolicyId, AcknowledgedAt = DateTime.UtcNow, IpAddress = ipAddress });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OnboardingProgressDto> GetProgressAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var tasks = await dbContext.OnboardingTasks.AsNoTracking().Where(x => x.PlanId == planId).ToListAsync(cancellationToken);
        var total = tasks.Count; var completed = tasks.Count(x => x.Status == "Completed"); var mandatory = tasks.Count(x => x.IsMandatory); var mandatoryCompleted = tasks.Count(x => x.IsMandatory && x.Status == "Completed");
        return new OnboardingProgressDto(planId, total, completed, mandatory, mandatoryCompleted, total == 0 ? 0 : Math.Round(completed * 100m / total, 2));
    }

    private Guid? CurrentUserGuidOrNull() => Guid.TryParse(currentUser.UserId, out var id) ? id : null;
    private void AddAudit(string module, string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "OnboardingPlan", EntityId = id.ToString(), Outcome = "Success" });
    private static OnboardingPlanDto MapPlan(OnboardingPlan x) => new(x.Id, x.EmployeeId, x.TemplateId, x.StartDate, x.Status, x.CompletedAt, x.Tasks.Count, x.Tasks.Count(t => t.Status == "Completed"));
    private static OnboardingTaskDto MapTask(OnboardingTask x) => new(x.Id, x.ChecklistItem, x.OwnerType, x.OwnerUserId, x.DueDate, x.Status, x.IsMandatory);
}
