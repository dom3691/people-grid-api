using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Training.DTOs;
using PeopleGrid.Application.Features.Training.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public sealed class TrainingService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : ITrainingService
{
    public async Task<TrainingProgramDto> CreateProgramAsync(TrainingProgramRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateProgram(request);
        var entity = new TrainingProgram { Title = request.Title.Trim(), Description = request.Description, ProviderId = request.ProviderId, Venue = request.Venue, StartDate = request.StartDate, EndDate = request.EndDate, Cost = request.Cost, Capacity = request.Capacity, TargetDepartmentId = request.TargetDepartmentId, TargetGradeLevelId = request.TargetGradeLevelId, TargetSkillId = request.TargetSkillId, Status = request.Status };
        dbContext.TrainingPrograms.Add(entity); AddAudit("CreateProgram", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<TrainingProgramDto>> ListProgramsAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.TrainingPrograms.AsNoTracking().OrderByDescending(x => x.StartDate).Select(x => Map(x)).ToListAsync(cancellationToken); }
    public async Task<TrainingProgramDto> GetProgramAsync(Guid id, CancellationToken cancellationToken = default) { EnsureView(); return Map(await dbContext.TrainingPrograms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Training program was not found.")); }

    public async Task<TrainingProgramDto> UpdateProgramAsync(Guid id, TrainingProgramRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateProgram(request);
        var entity = await dbContext.TrainingPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Training program was not found.");
        entity.Title = request.Title.Trim(); entity.Description = request.Description; entity.ProviderId = request.ProviderId; entity.Venue = request.Venue; entity.StartDate = request.StartDate; entity.EndDate = request.EndDate; entity.Cost = request.Cost; entity.Capacity = request.Capacity; entity.TargetDepartmentId = request.TargetDepartmentId; entity.TargetGradeLevelId = request.TargetGradeLevelId; entity.TargetSkillId = request.TargetSkillId; entity.Status = request.Status;
        AddAudit("UpdateProgram", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<object>> NominateEmployeesAsync(Guid id, TrainingNominationRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); var program = await dbContext.TrainingPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Training program was not found.");
        var currentCount = await dbContext.TrainingNominations.CountAsync(x => x.ProgramId == id && x.Status != "Rejected", cancellationToken);
        if (program.Capacity > 0 && currentCount + request.EmployeeIds.Distinct().Count() > program.Capacity) throw new BusinessRuleException("Training capacity exceeded.");
        var results = new List<object>();
        foreach (var employeeId in request.EmployeeIds.Distinct())
        {
            if (!await dbContext.Employees.AnyAsync(x => x.Id == employeeId && x.Status == "Active", cancellationToken)) throw new BusinessRuleException("Nominee must be active employee.");
            if (await dbContext.TrainingNominations.AnyAsync(x => x.ProgramId == id && x.EmployeeId == employeeId, cancellationToken)) continue;
            var nomination = new TrainingNomination { ProgramId = id, EmployeeId = employeeId, NominatedBy = CurrentUserGuid() };
            dbContext.TrainingNominations.Add(nomination); results.Add(new { nomination.Id, employeeId, nomination.Status });
        }
        AddAudit("NominateEmployees", id); await dbContext.SaveChangesAsync(cancellationToken); return results;
    }

    public async Task<object> ApproveNominationAsync(Guid id, TrainingDecisionRequest request, CancellationToken cancellationToken = default) => await DecideNominationAsync(id, "Approved", request.Comments, cancellationToken);
    public async Task<object> RejectNominationAsync(Guid id, TrainingDecisionRequest request, CancellationToken cancellationToken = default) => await DecideNominationAsync(id, "Rejected", request.Comments, cancellationToken);

    public async Task<object> RecordAttendanceAsync(Guid id, TrainingAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!await dbContext.TrainingNominations.AnyAsync(x => x.ProgramId == id && x.EmployeeId == request.EmployeeId && x.Status == "Approved", cancellationToken)) throw new BusinessRuleException("Attendance only allowed for approved participants.");
        dbContext.TrainingAttendance.Add(new TrainingAttendance { ProgramId = id, EmployeeId = request.EmployeeId, SessionDate = request.SessionDate, Attended = request.Attended, MarkedBy = CurrentUserGuid() });
        AddAudit("RecordAttendance", id); await dbContext.SaveChangesAsync(cancellationToken); return new { ProgramId = id, request.EmployeeId, request.Attended };
    }

    public async Task<object> SubmitFeedbackAsync(Guid id, TrainingFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Score is < 1 or > 5) throw new BusinessRuleException("Feedback score must be between 1 and 5.");
        if (!await dbContext.TrainingNominations.AnyAsync(x => x.ProgramId == id && x.EmployeeId == request.EmployeeId && x.Status == "Approved", cancellationToken)) throw new BusinessRuleException("Feedback only allowed for approved participants.");
        dbContext.TrainingFeedback.Add(new TrainingFeedback { ProgramId = id, EmployeeId = request.EmployeeId, Score = request.Score, Comments = request.Comments });
        await dbContext.SaveChangesAsync(cancellationToken); return new { ProgramId = id, request.EmployeeId, request.Score };
    }

    public async Task<object> UploadCertificateAsync(Guid id, TrainingCertificateRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!await dbContext.TrainingAttendance.AnyAsync(x => x.ProgramId == id && x.EmployeeId == request.EmployeeId && x.Attended, cancellationToken)) throw new BusinessRuleException("Certificate upload requires completed attendance.");
        dbContext.TrainingCertificates.Add(new TrainingCertificate { ProgramId = id, EmployeeId = request.EmployeeId, FileName = request.FileName.Trim(), StorageKey = request.StorageKey.Trim(), IssuedDate = request.IssuedDate });
        dbContext.TrainingHistories.Add(new TrainingHistory { ProgramId = id, EmployeeId = request.EmployeeId, Status = "Completed", CompletedAt = DateTime.UtcNow });
        var skillId = await dbContext.TrainingPrograms.Where(x => x.Id == id).Select(x => x.TargetSkillId).FirstOrDefaultAsync(cancellationToken);
        if (skillId is not null && !await dbContext.EmployeeSkills.AnyAsync(x => x.EmployeeId == request.EmployeeId && x.SkillId == skillId, cancellationToken))
            dbContext.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = request.EmployeeId, SkillId = skillId.Value, ProficiencyLevel = "Beginner", AcquiredDate = request.IssuedDate, Source = "Training" });
        AddAudit("UploadCertificate", id); await dbContext.SaveChangesAsync(cancellationToken); return new { ProgramId = id, request.EmployeeId, Status = "Completed" };
    }

    public async Task<IReadOnlyCollection<TrainingHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await dbContext.TrainingHistories.AsNoTracking().Where(x => x.EmployeeId == employeeId).Select(x => new TrainingHistoryDto(x.EmployeeId, x.ProgramId, x.Status, x.CompletedAt)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmployeeSkillDto>> GetSkillsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EnsureView();
        return await dbContext.EmployeeSkills.AsNoTracking().Include(x => x.Skill).Where(x => x.EmployeeId == employeeId).Select(x => new EmployeeSkillDto(x.EmployeeId, x.SkillId, x.Skill!.Name, x.ProficiencyLevel, x.AcquiredDate, x.Source)).ToListAsync(cancellationToken);
    }

    private async Task<object> DecideNominationAsync(Guid id, string decision, string? comments, CancellationToken ct)
    {
        EnsureManage(); var nomination = await dbContext.TrainingNominations.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Training nomination was not found.");
        nomination.Status = decision; dbContext.TrainingApprovalActions.Add(new TrainingApprovalAction { NominationId = id, ActorUserId = CurrentUserGuid(), Decision = decision, Comments = comments });
        AddAudit($"{decision}Nomination", id); await dbContext.SaveChangesAsync(ct); return new { nomination.Id, nomination.Status };
    }

    private static void ValidateProgram(TrainingProgramRequest request) { if (request.EndDate < request.StartDate) throw new BusinessRuleException("Training end date cannot be before start date."); if (request.Cost < 0) throw new BusinessRuleException("Training cost must be non-negative."); if (request.Capacity < 0) throw new BusinessRuleException("Capacity cannot be negative."); }
    private static TrainingProgramDto Map(TrainingProgram x) => new(x.Id, x.Title, x.StartDate, x.EndDate, x.Cost, x.Capacity, x.Status);
    private void AddAudit(string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = "Training", Action = action, EntityType = "Training", EntityId = id.ToString(), Outcome = "Success" });
    private void EnsureView() { if (!currentUser.Permissions.Contains("Training.View") && !currentUser.Permissions.Contains("Training.Manage")) throw new ForbiddenException("Training view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Training.Manage")) throw new ForbiddenException("Training management permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}

