using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Approvals.DTOs;
using PeopleGrid.Application.Features.Approvals.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class ApprovalWorkflowService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IApprovalWorkflowService
{
    public async Task<ApprovalFlowDto> CreateFlowAsync(CreateApprovalFlowRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateFlowReferencesAsync(request.RequestTypeId, request.DepartmentId, request.Steps, cancellationToken);
        var flow = new ApprovalFlow { Name = request.Name.Trim(), Module = "HRRequests", RequestTypeId = request.RequestTypeId, DepartmentId = request.DepartmentId, IsActive = true };
        foreach (var step in request.Steps.OrderBy(x => x.Sequence))
        {
            flow.Steps.Add(MapStep(step));
        }
        dbContext.ApprovalFlows.Add(flow);
        AddAudit("Approvals", "CreateFlow", "ApprovalFlow", flow.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetFlowByIdAsync(flow.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<ApprovalFlowDto>> ListFlowsAsync(ApprovalFlowListQuery query, CancellationToken cancellationToken = default)
    {
        var source = LoadFlows();
        if (query.RequestTypeId is not null) source = source.Where(x => x.RequestTypeId == query.RequestTypeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.DepartmentId == query.DepartmentId);
        if (query.IsActive is not null) source = source.Where(x => x.IsActive == query.IsActive);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var flows = await source.OrderBy(x => x.Name).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<ApprovalFlowDto>(flows.Select(MapFlow).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<ApprovalFlowDto> GetFlowByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flow = await LoadFlows().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Approval flow was not found.");
        return MapFlow(flow);
    }

    public async Task<ApprovalFlowDto> UpdateFlowAsync(Guid id, UpdateApprovalFlowRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateFlowReferencesAsync(request.RequestTypeId, request.DepartmentId, request.Steps, cancellationToken);
        var flow = await dbContext.ApprovalFlows.Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Approval flow was not found.");
        flow.Name = request.Name.Trim();
        flow.RequestTypeId = request.RequestTypeId;
        flow.DepartmentId = request.DepartmentId;
        dbContext.ApprovalSteps.RemoveRange(flow.Steps);
        foreach (var step in request.Steps.OrderBy(x => x.Sequence))
        {
            dbContext.ApprovalSteps.Add(MapStep(step, flow.Id));
        }
        AddAudit("Approvals", "UpdateFlow", "ApprovalFlow", flow.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetFlowByIdAsync(id, cancellationToken);
    }

    public async Task<ApprovalFlowDto> ChangeFlowStatusAsync(Guid id, ChangeApprovalFlowStatusRequest request, CancellationToken cancellationToken = default)
    {
        var flow = await dbContext.ApprovalFlows.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Approval flow was not found.");
        flow.IsActive = request.IsActive;
        AddAudit("Approvals", request.IsActive ? "ActivateFlow" : "DeactivateFlow", "ApprovalFlow", flow.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetFlowByIdAsync(id, cancellationToken);
    }

    public async Task<ApprovalInstanceDto> CreateInstanceAsync(CreateApprovalInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var hrRequest = await dbContext.HRRequests.Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo).FirstOrDefaultAsync(x => x.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        if (await dbContext.ApprovalInstances.AnyAsync(x => x.RequestId == request.RequestId, cancellationToken))
        {
            throw new BusinessRuleException("Approval instance already exists for this request.");
        }

        var flow = await ResolveFlowAsync(hrRequest, cancellationToken);
        var steps = await dbContext.ApprovalSteps.Include(x => x.ApproverRole).Where(x => x.ApprovalFlowId == flow.Id && x.IsActive).OrderBy(x => x.Sequence).ToListAsync(cancellationToken);
        if (steps.Count == 0) throw new BusinessRuleException("Approval flow must have at least one active step.");

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var firstStep = steps[0];
        var instance = new ApprovalInstance { RequestId = hrRequest.Id, ApprovalFlowId = flow.Id, Status = "Pending", CurrentStepId = firstStep.Id, StartedAt = DateTime.UtcNow };
        dbContext.ApprovalInstances.Add(instance);
        dbContext.ApprovalInstanceSteps.Add(new ApprovalInstanceStep
        {
            InstanceId = instance.Id,
            StepId = firstStep.Id,
            AssignedUserId = await ResolveApproverAsync(hrRequest, firstStep, cancellationToken),
            Status = "Pending",
            AssignedAt = DateTime.UtcNow
        });
        AddAudit("Approvals", "CreateInstance", "ApprovalInstance", instance.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return MapInstance(instance);
    }

    public async Task<IReadOnlyCollection<PendingApprovalDto>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserGuid();
        var items = await dbContext.ApprovalInstanceSteps.AsNoTracking()
            .Include(x => x.Instance!).ThenInclude(x => x!.Request)
            .Include(x => x.Step)
            .Where(x => x.Status == "Pending" && x.AssignedUserId == userId)
            .OrderBy(x => x.AssignedAt)
            .Select(x => new PendingApprovalDto(x.Id, x.InstanceId, x.Instance!.RequestId, x.Instance.Request!.RequestNumber, x.Instance.Request.Subject, x.Status, x.AssignedAt, x.Step!.SlaHours))
            .ToListAsync(cancellationToken);
        return items;
    }

    public Task<ApprovalInstanceDto> ApproveAsync(Guid approvalInstanceStepId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default)
        => DecideAsync(approvalInstanceStepId, "Approved", request.Comments, cancellationToken);

    public Task<ApprovalInstanceDto> RejectAsync(Guid approvalInstanceStepId, ApprovalDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comments)) throw new BusinessRuleException("Reject action requires comment.");
        return DecideAsync(approvalInstanceStepId, "Rejected", request.Comments, cancellationToken);
    }

    public async Task EscalateAsync(Guid approvalInstanceStepId, EscalateApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var step = await dbContext.ApprovalInstanceSteps.FirstOrDefaultAsync(x => x.Id == approvalInstanceStepId, cancellationToken)
            ?? throw new NotFoundException("Approval step was not found.");
        if (!await dbContext.Users.AnyAsync(x => x.Id == request.EscalatedToUserId && x.IsActive, cancellationToken))
            throw new BusinessRuleException("Escalation user is invalid.");
        step.AssignedUserId = request.EscalatedToUserId;
        dbContext.ApprovalEscalations.Add(new ApprovalEscalation { ApprovalInstanceStepId = step.Id, EscalatedToUserId = request.EscalatedToUserId, EscalatedAt = DateTime.UtcNow, Reason = request.Reason.Trim() });
        AddAudit("Approvals", "Escalate", "ApprovalInstanceStep", step.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ApprovalHistoryDto>> GetHistoryAsync(Guid approvalInstanceId, CancellationToken cancellationToken = default)
    {
        var actions = await dbContext.ApprovalActions.AsNoTracking().Include(x => x.Step)
            .Where(x => x.ApprovalInstanceId == approvalInstanceId)
            .OrderBy(x => x.DecidedAt)
            .Select(x => new ApprovalHistoryDto(x.Id, x.Decision, x.Comments, x.ActorUserId, x.DecidedAt, x.Step!.Sequence))
            .ToListAsync(cancellationToken);
        return actions;
    }

    private async Task<ApprovalInstanceDto> DecideAsync(Guid approvalInstanceStepId, string decision, string? comments, CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuid();
        var instanceStep = await dbContext.ApprovalInstanceSteps
            .Include(x => x.Step)
            .Include(x => x.Instance!).ThenInclude(x => x!.Request)
            .FirstOrDefaultAsync(x => x.Id == approvalInstanceStepId, cancellationToken)
            ?? throw new NotFoundException("Approval step was not found.");

        if (instanceStep.Status != "Pending") throw new BusinessRuleException("Approval step is not pending.");
        if (instanceStep.AssignedUserId != userId && !currentUser.Permissions.Contains("Approval.Manage"))
            throw new ForbiddenException("Approval action is allowed only for the current assigned approver.");

        var instance = instanceStep.Instance!;
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        instanceStep.Status = decision;
        instanceStep.CompletedAt = DateTime.UtcNow;
        dbContext.ApprovalActions.Add(new ApprovalAction { ApprovalInstanceId = instance.Id, StepId = instanceStep.StepId, ActorUserId = userId, Decision = decision, Comments = comments?.Trim(), DecidedAt = DateTime.UtcNow });

        if (decision == "Rejected")
        {
            instance.Status = "Rejected";
            instance.CompletedAt = DateTime.UtcNow;
            instance.Request!.Status = "Rejected";
            AddRequestHistory(instance.Request, "Rejected", comments);
        }
        else
        {
            var nextStep = await dbContext.ApprovalSteps.Where(x => x.ApprovalFlowId == instance.ApprovalFlowId && x.IsActive && x.Sequence > instanceStep.Step!.Sequence).OrderBy(x => x.Sequence).FirstOrDefaultAsync(cancellationToken);
            if (nextStep is null)
            {
                instance.Status = "Approved";
                instance.CompletedAt = DateTime.UtcNow;
                instance.Request!.Status = "Approved";
                AddRequestHistory(instance.Request, "Approved", comments);
            }
            else
            {
                instance.CurrentStepId = nextStep.Id;
                dbContext.ApprovalInstanceSteps.Add(new ApprovalInstanceStep { InstanceId = instance.Id, StepId = nextStep.Id, AssignedUserId = await ResolveApproverAsync(instance.Request!, nextStep, cancellationToken), Status = "Pending", AssignedAt = DateTime.UtcNow });
            }
        }

        AddAudit("Approvals", decision, "ApprovalInstance", instance.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return MapInstance(instance);
    }

    private async Task<ApprovalFlow> ResolveFlowAsync(HRRequest request, CancellationToken cancellationToken)
    {
        var departmentId = request.Employee?.EmploymentInfo?.DepartmentId;
        var flow = await LoadFlows()
            .Where(x => x.IsActive && x.RequestTypeId == request.RequestTypeId && (x.DepartmentId == departmentId || x.DepartmentId == null))
            .OrderByDescending(x => x.DepartmentId != null)
            .FirstOrDefaultAsync(cancellationToken);
        return flow ?? throw new BusinessRuleException("No active approval flow is configured for this request.");
    }

    private async Task<Guid?> ResolveApproverAsync(HRRequest request, ApprovalStep step, CancellationToken cancellationToken)
    {
        if (step.ApproverType == "User") return step.ApproverUserId;
        if (step.ApproverType == "LineManager") return request.Employee?.EmploymentInfo?.LineManagerId;
        if (step.ApproverType == "Role" && step.ApproverRoleId is not null)
        {
            return await dbContext.Users.Where(x => x.IsActive && x.UserRoles.Any(ur => ur.RoleId == step.ApproverRoleId)).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        }
        if (step.ApproverType == "HR")
        {
            return await dbContext.Users.Where(x => x.IsActive && x.UserRoles.Any(ur => ur.Role!.Code == "HR_ADMIN")).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        }
        throw new BusinessRuleException("Unable to resolve approver for approval step.");
    }

    private async Task ValidateFlowReferencesAsync(Guid? requestTypeId, Guid? departmentId, IReadOnlyCollection<ApprovalFlowStepRequest> steps, CancellationToken cancellationToken)
    {
        if (requestTypeId is not null && !await dbContext.HRRequestTypes.AnyAsync(x => x.Id == requestTypeId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected request type is invalid.");
        if (departmentId is not null && !await dbContext.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected department is invalid.");
        foreach (var step in steps)
        {
            if (step.ApproverRoleId is not null && !await dbContext.Roles.AnyAsync(x => x.Id == step.ApproverRoleId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Approver role is invalid.");
            if (step.ApproverUserId is not null && !await dbContext.Users.AnyAsync(x => x.Id == step.ApproverUserId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Approver user is invalid.");
        }
    }

    private IQueryable<ApprovalFlow> LoadFlows() => dbContext.ApprovalFlows.AsNoTracking()
        .Include(x => x.RequestTypeDefinition)
        .Include(x => x.Department)
        .Include(x => x.Steps).ThenInclude(x => x.ApproverRole)
        .Include(x => x.Steps).ThenInclude(x => x.ApproverUser);

    private static ApprovalStep MapStep(ApprovalFlowStepRequest request, Guid? flowId = null) => new() { ApprovalFlowId = flowId ?? Guid.Empty, Sequence = request.Sequence, ApproverType = request.ApproverType, ApproverRoleId = request.ApproverRoleId, ApproverUserId = request.ApproverUserId, SlaHours = request.SlaHours, IsActive = true };
    private static ApprovalFlowDto MapFlow(ApprovalFlow x) => new(x.Id, x.Name, x.RequestTypeId, x.RequestTypeDefinition?.Name, x.DepartmentId, x.Department?.Name, x.IsActive, x.Steps.OrderBy(s => s.Sequence).Select(s => new ApprovalFlowStepDto(s.Id, s.Sequence, s.ApproverType, s.ApproverRoleId, s.ApproverRole?.Name, s.ApproverUserId, s.ApproverUser is null ? null : $"{s.ApproverUser.FirstName} {s.ApproverUser.LastName}".Trim(), s.SlaHours, s.IsActive)).ToList());
    private static ApprovalInstanceDto MapInstance(ApprovalInstance x) => new(x.Id, x.RequestId, x.Status, x.CurrentStepId, x.StartedAt, x.CompletedAt);
    private void AddRequestHistory(HRRequest request, string newStatus, string? comments) => dbContext.HRRequestStatusHistories.Add(new HRRequestStatusHistory { RequestId = request.Id, OldStatus = request.Status, NewStatus = newStatus, Comments = comments?.Trim(), ChangedBy = CurrentUserGuidOrNull(), ChangedAt = DateTime.UtcNow });
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
    private Guid? CurrentUserGuidOrNull() => Guid.TryParse(currentUser.UserId, out var id) ? id : null;
    private void AddAudit(string module, string action, string entityType, Guid entityId) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = entityType, EntityId = entityId.ToString(), Outcome = "Success" });
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) => dbContext is DbContext ef && ef.Database.CurrentTransaction is null ? await ef.Database.BeginTransactionAsync(cancellationToken) : null;
}
