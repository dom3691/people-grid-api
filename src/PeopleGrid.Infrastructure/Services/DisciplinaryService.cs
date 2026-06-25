using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Disciplinary.DTOs;
using PeopleGrid.Application.Features.Disciplinary.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class DisciplinaryService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IDisciplinaryService
{
    public async Task<DisciplinaryCaseDto> CreateCaseAsync(CreateDisciplinaryCaseRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!await dbContext.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken)) throw new BusinessRuleException("Employee is invalid.");
        var entity = new DisciplinaryCase { CaseNumber = await GenerateCaseNumberAsync(cancellationToken), EmployeeId = request.EmployeeId, IncidentDate = request.IncidentDate, Category = request.Category.Trim(), QueryDetails = request.QueryDetails.Trim(), Status = "Open", IssueDate = request.IssueDate, ResponseDueDate = request.ResponseDueDate, IssuedBy = CurrentUserGuid() };
        dbContext.DisciplinaryCases.Add(entity);
        AddAudit("Disciplinary", "CreateCase", entity.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<PaginatedResponse<DisciplinaryCaseDto>> ListCasesAsync(DisciplinaryCaseQuery query, CancellationToken cancellationToken = default)
    {
        EnsureView();
        var source = dbContext.DisciplinaryCases.AsNoTracking().Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee!.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.Category)) source = source.Where(x => x.Category == query.Category);
        if (query.FromDate is not null) source = source.Where(x => x.IncidentDate >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.IncidentDate <= query.ToDate);
        var total = await source.CountAsync(cancellationToken); var page = query.ToPagination();
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<DisciplinaryCaseDto>(rows.Select(Map).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<DisciplinaryCaseDto> GetCaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DisciplinaryCases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Disciplinary case was not found.");
        await EnsureCanViewCaseAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> RespondAsync(Guid id, DisciplinaryResponseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DisciplinaryCases.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Disciplinary case was not found.");
        if (entity.Status != "Open") throw new BusinessRuleException("Only open cases can be responded to.");
        if (entity.Employee?.UserId != CurrentUserGuid()) throw new ForbiddenException("Employee can only respond to own cases.");
        dbContext.DisciplinaryResponses.Add(new DisciplinaryResponse { CaseId = id, ResponseText = request.ResponseText.Trim(), SubmittedBy = CurrentUserGuid(), SubmittedAt = DateTime.UtcNow });
        entity.Status = "Employee Responded";
        AddAudit("Disciplinary", "Respond", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> ReviewAsync(Guid id, DisciplinaryReviewRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = await LoadCaseAsync(id, cancellationToken);
        if (entity.Status is not ("Employee Responded" or "Open")) throw new BusinessRuleException("Case is not ready for review.");
        dbContext.DisciplinaryReviews.Add(new DisciplinaryReview { CaseId = id, ReviewComments = request.ReviewComments.Trim(), Outcome = request.Outcome.Trim(), ReviewedBy = CurrentUserGuid(), ReviewedAt = DateTime.UtcNow });
        entity.Status = "Under Review";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> IssueWarningAsync(Guid id, WarningLetterRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await LoadCaseAsync(id, cancellationToken);
        dbContext.WarningLetters.Add(new WarningLetter { CaseId = id, WarningLevel = request.WarningLevel.Trim(), LetterContent = request.LetterContent.Trim(), IssuedBy = CurrentUserGuid(), IssuedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> RecordSuspensionAsync(Guid id, SuspensionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await LoadCaseAsync(id, cancellationToken);
        dbContext.SuspensionRecords.Add(new SuspensionRecord { CaseId = id, StartDate = request.StartDate, EndDate = request.EndDate, Reason = request.Reason.Trim(), ApprovedBy = CurrentUserGuid() });
        await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> EscalateAsync(Guid id, EscalateDisciplinaryRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await LoadCaseAsync(id, cancellationToken);
        dbContext.DisciplinaryEscalations.Add(new DisciplinaryEscalation { CaseId = id, EscalatedTo = request.EscalatedTo, EscalatedBy = CurrentUserGuid(), Reason = request.Reason.Trim(), EscalatedAt = DateTime.UtcNow });
        entity.Status = "Escalated";
        await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<DisciplinaryCaseDto> CloseAsync(Guid id, CloseDisciplinaryCaseRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var entity = await LoadCaseAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.FinalOutcome)) throw new BusinessRuleException("Final outcome is required.");
        dbContext.DisciplinaryReviews.Add(new DisciplinaryReview { CaseId = id, ReviewComments = request.Comments.Trim(), Outcome = request.FinalOutcome.Trim(), ReviewedBy = CurrentUserGuid(), ReviewedAt = DateTime.UtcNow });
        entity.Status = "Closed";
        await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<DisciplinaryCaseDto>> EmployeeHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await dbContext.DisciplinaryCases.AsNoTracking().Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.IssueDate).Select(x => Map(x)).ToListAsync(cancellationToken);
    }

    private async Task<DisciplinaryCase> LoadCaseAsync(Guid id, CancellationToken ct) => await dbContext.DisciplinaryCases.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Disciplinary case was not found.");
    private async Task EnsureCanViewCaseAsync(DisciplinaryCase entity, CancellationToken ct) { if (currentUser.Permissions.Contains("Disciplinary.View") || currentUser.Permissions.Contains("Disciplinary.Manage")) return; var userId = CurrentUserGuid(); if (!await dbContext.Employees.AnyAsync(x => x.Id == entity.EmployeeId && x.UserId == userId, ct)) throw new ForbiddenException("You are not allowed to view this disciplinary case."); }
    private void EnsureView() { if (!currentUser.Permissions.Contains("Disciplinary.View") && !currentUser.Permissions.Contains("Disciplinary.Manage")) throw new ForbiddenException("Disciplinary view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Disciplinary.Manage")) throw new ForbiddenException("Disciplinary management permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
    private async Task<string> GenerateCaseNumberAsync(CancellationToken ct) { var count = await dbContext.DisciplinaryCases.IgnoreQueryFilters().CountAsync(ct) + 1; string number; do { number = $"DSC-{DateTime.UtcNow:yyyyMMdd}-{count:00000}"; count++; } while (await dbContext.DisciplinaryCases.IgnoreQueryFilters().AnyAsync(x => x.CaseNumber == number, ct)); return number; }
    private void AddAudit(string module, string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "DisciplinaryCase", EntityId = id.ToString(), Outcome = "Success" });
    private static DisciplinaryCaseDto Map(DisciplinaryCase x) => new(x.Id, x.CaseNumber, x.EmployeeId, x.IncidentDate, x.Category, x.QueryDetails, x.Status, x.ResponseDueDate, x.IssueDate);
}
