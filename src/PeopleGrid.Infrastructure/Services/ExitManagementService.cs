using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.ExitManagement.DTOs;
using PeopleGrid.Application.Features.ExitManagement.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class ExitManagementService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IExitManagementService
{
    private static readonly HashSet<string> SettlementStatuses = ["Pending", "In Progress", "Processed", "Not Applicable"];

    public async Task<ExitCaseDto> SubmitResignationAsync(SubmitResignationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProposedLastWorkingDay <= request.ResignationDate) throw new BusinessRuleException("Proposed last working day must be after resignation date.");
        if (!request.HrOverride && request.ProposedLastWorkingDay < request.ResignationDate.AddDays(request.NoticePeriod)) throw new BusinessRuleException("Proposed last working day does not satisfy notice period.");
        if (!await dbContext.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken)) throw new BusinessRuleException("Employee is invalid.");

        var exitCase = new ExitCase { CaseNumber = await GenerateCaseNumberAsync(cancellationToken), EmployeeId = request.EmployeeId, ResignationDate = request.ResignationDate, LastWorkingDay = request.ProposedLastWorkingDay, Reason = request.Reason.Trim(), NoticePeriod = request.NoticePeriod, Status = "Pending Approval" };
        dbContext.ExitCases.Add(exitCase);
        dbContext.ResignationRequests.Add(new ResignationRequest { ExitCaseId = exitCase.Id, EmployeeId = request.EmployeeId, ProposedLastWorkingDay = request.ProposedLastWorkingDay, ReasonForLeaving = request.Reason.Trim() });
        dbContext.FinalSettlementStatuses.Add(new FinalSettlementStatus { ExitCaseId = exitCase.Id, Status = "Pending", StatusUpdatedBy = CurrentUserGuid() });
        AddAudit("SubmitResignation", exitCase.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(exitCase.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<ExitCaseDto>> ListAsync(ExitCaseQuery query, CancellationToken cancellationToken = default)
    {
        EnsureView();
        var source = dbContext.ExitCases.AsNoTracking().Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee!.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (query.FromDate is not null) source = source.Where(x => x.ResignationDate >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.ResignationDate <= query.ToDate);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        var mapped = new List<ExitCaseDto>();
        foreach (var row in rows) mapped.Add(await MapAsync(row.Id, cancellationToken));
        return new PaginatedResponse<ExitCaseDto>(mapped, page.PageNumber, page.Take, total);
    }

    public async Task<ExitCaseDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> ApproveAsync(Guid id, ExitDecisionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = await LoadAsync(id, cancellationToken);
        if (entity.Status != "Pending Approval") throw new BusinessRuleException("Only pending exit cases can be approved.");
        var step = await dbContext.ExitApprovalActions.CountAsync(x => x.ExitCaseId == id, cancellationToken) + 1;
        dbContext.ExitApprovalActions.Add(new ExitApprovalAction { ExitCaseId = id, Step = step, ActorUserId = CurrentUserGuid(), Decision = "Approved", Comments = request.Comments });
        entity.Status = step >= 2 ? "Approved" : "Pending HR Approval";
        if (entity.Status == "Pending HR Approval") entity.Status = "Approved";
        AddNotification(entity.EmployeeId, "Exit approved", "Your exit request has been approved.");
        AddAudit("Approve", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> RejectAsync(Guid id, ExitDecisionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (string.IsNullOrWhiteSpace(request.Comments)) throw new BusinessRuleException("Rejection comments are required.");
        var entity = await LoadAsync(id, cancellationToken);
        dbContext.ExitApprovalActions.Add(new ExitApprovalAction { ExitCaseId = id, Step = 1, ActorUserId = CurrentUserGuid(), Decision = "Rejected", Comments = request.Comments.Trim() });
        entity.Status = "Rejected";
        AddNotification(entity.EmployeeId, "Exit rejected", request.Comments.Trim());
        AddAudit("Reject", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> AddClearanceItemAsync(Guid id, AddExitClearanceItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        _ = await LoadAsync(id, cancellationToken);
        if (request.OwnerUserId is not null && !await dbContext.Users.AnyAsync(x => x.Id == request.OwnerUserId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Clearance owner must be an active user.");
        dbContext.ExitClearanceItems.Add(new ExitClearanceItem { ExitCaseId = id, ItemName = request.ItemName.Trim(), OwnerUserId = request.OwnerUserId, IsMandatory = request.IsMandatory });
        AddAudit("AddClearanceItem", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> CompleteClearanceItemAsync(Guid id, CompleteExitClearanceItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var item = await dbContext.ExitClearanceItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Clearance item was not found.");
        item.Status = "Completed";
        item.CompletedAt = DateTime.UtcNow;
        AddAudit("CompleteClearanceItem", item.ExitCaseId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(item.ExitCaseId, cancellationToken);
    }

    public async Task<ExitCaseDto> RecordHandoverAsync(Guid id, ExitHandoverRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        _ = await LoadAsync(id, cancellationToken);
        dbContext.ExitHandoverRecords.Add(new ExitHandoverRecord { ExitCaseId = id, HandoverToUserId = request.HandoverToUserId, Notes = request.Notes.Trim(), CompletedAt = DateTime.UtcNow });
        AddAudit("RecordHandover", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> SubmitExitInterviewAsync(Guid id, ExitInterviewRequest request, CancellationToken cancellationToken = default)
    {
        _ = await LoadAsync(id, cancellationToken);
        foreach (var answer in request.Answers.Where(x => !string.IsNullOrWhiteSpace(x.Question)))
            dbContext.ExitInterviewResponses.Add(new ExitInterviewResponse { ExitCaseId = id, Question = answer.Question.Trim(), Response = answer.Response });
        AddAudit("SubmitExitInterview", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> UpdateFinalSettlementStatusAsync(Guid id, UpdateFinalSettlementStatusRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!SettlementStatuses.Contains(request.Status)) throw new BusinessRuleException("Final settlement status is invalid.");
        _ = await LoadAsync(id, cancellationToken);
        var settlement = await dbContext.FinalSettlementStatuses.FirstOrDefaultAsync(x => x.ExitCaseId == id, cancellationToken);
        if (settlement is null) dbContext.FinalSettlementStatuses.Add(new FinalSettlementStatus { ExitCaseId = id, Status = request.Status, Comments = request.Comments, StatusUpdatedBy = CurrentUserGuid(), StatusUpdatedAt = DateTime.UtcNow });
        else { settlement.Status = request.Status; settlement.Comments = request.Comments; settlement.StatusUpdatedBy = CurrentUserGuid(); settlement.StatusUpdatedAt = DateTime.UtcNow; }
        AddAudit("UpdateFinalSettlement", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> DeactivateAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = await dbContext.ExitCases.Include(x => x.Employee).ThenInclude(x => x!.User).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Exit case was not found.");
        if (entity.Status != "Approved" && !currentUser.Permissions.Contains("Exit.Manage")) throw new BusinessRuleException("Account deactivation requires approved exit or authorized override.");
        if (entity.Employee is not null)
        {
            entity.Employee.Status = "Exited";
            entity.Employee.DeactivatedAt = DateTime.UtcNow;
            entity.Employee.DeactivationReason = "Exit management";
            if (entity.Employee.User is not null) { entity.Employee.User.Status = "Inactive"; entity.Employee.User.IsActive = false; }
        }
        entity.Status = "Account Deactivated";
        AddAudit("DeactivateAccount", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ExitCaseDto> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = await LoadAsync(id, cancellationToken);
        var hasIncompleteMandatory = await dbContext.ExitClearanceItems.AnyAsync(x => x.ExitCaseId == id && x.IsMandatory && x.Status != "Completed", cancellationToken);
        if (hasIncompleteMandatory) throw new BusinessRuleException("Mandatory clearance items must be completed before closure.");
        entity.Status = "Closed";
        AddAudit("Close", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    private async Task<ExitCase> LoadAsync(Guid id, CancellationToken ct) => await dbContext.ExitCases.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Exit case was not found.");
    private async Task<ExitCaseDto> MapAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.ExitCases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Exit case was not found.");
        var mandatory = await dbContext.ExitClearanceItems.CountAsync(x => x.ExitCaseId == id && x.IsMandatory, ct);
        var completed = await dbContext.ExitClearanceItems.CountAsync(x => x.ExitCaseId == id && x.IsMandatory && x.Status == "Completed", ct);
        return new ExitCaseDto(entity.Id, entity.CaseNumber, entity.EmployeeId, entity.ResignationDate, entity.LastWorkingDay, entity.Reason, entity.NoticePeriod, entity.Status, mandatory, completed);
    }

    private async Task<string> GenerateCaseNumberAsync(CancellationToken ct) { var count = await dbContext.ExitCases.IgnoreQueryFilters().CountAsync(ct) + 1; string number; do { number = $"EXT-{DateTime.UtcNow:yyyyMMdd}-{count:00000}"; count++; } while (await dbContext.ExitCases.IgnoreQueryFilters().AnyAsync(x => x.CaseNumber == number, ct)); return number; }
    private void AddNotification(Guid employeeId, string title, string message) { var userId = dbContext.Employees.AsNoTracking().Where(x => x.Id == employeeId).Select(x => x.UserId).FirstOrDefault(); if (userId is not null) dbContext.Notifications.Add(new Notification { RecipientUserId = userId.Value, Title = title, Message = message, Type = "Exit", RelatedEntityType = "ExitCase" }); }
    private void AddAudit(string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = "Exit", Action = action, EntityType = "ExitCase", EntityId = id.ToString(), Outcome = "Success" });
    private void EnsureView() { if (!currentUser.Permissions.Contains("Exit.View") && !currentUser.Permissions.Contains("Exit.Manage")) throw new ForbiddenException("Exit view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Exit.Manage")) throw new ForbiddenException("Exit management permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}

