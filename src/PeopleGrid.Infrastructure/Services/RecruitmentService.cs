using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Recruitment.DTOs;
using PeopleGrid.Application.Features.Recruitment.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public sealed class RecruitmentService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRecruitmentService
{
    private static readonly string[] Pipeline = ["Applied", "Shortlisted", "Interview Scheduled", "Interviewed", "Selected", "Rejected", "Offer Sent", "Hired"];

    public async Task<JobOpeningDto> CreateJobOpeningAsync(JobOpeningRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateJob(request);
        var entity = new JobOpening { Title = request.Title.Trim(), DepartmentId = request.DepartmentId, BranchId = request.BranchId, HiringManagerId = request.HiringManagerId, Vacancies = request.Vacancies, EmploymentType = request.EmploymentType.Trim(), GradeLevelId = request.GradeLevelId, JobDescription = request.JobDescription.Trim(), Requirements = request.Requirements.Trim(), PublicationDate = request.PublicationDate, ClosingDate = request.ClosingDate, Status = request.Status };
        dbContext.JobOpenings.Add(entity); AddAudit("CreateJobOpening", entity.Id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<IReadOnlyCollection<JobOpeningDto>> ListJobOpeningsAsync(CancellationToken cancellationToken = default) { EnsureView(); return await dbContext.JobOpenings.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => Map(x)).ToListAsync(cancellationToken); }

    public async Task<JobOpeningDto> UpdateJobOpeningAsync(Guid id, JobOpeningRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage(); ValidateJob(request);
        var entity = await dbContext.JobOpenings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Job opening was not found.");
        entity.Title = request.Title.Trim(); entity.DepartmentId = request.DepartmentId; entity.BranchId = request.BranchId; entity.HiringManagerId = request.HiringManagerId; entity.Vacancies = request.Vacancies; entity.EmploymentType = request.EmploymentType.Trim(); entity.GradeLevelId = request.GradeLevelId; entity.JobDescription = request.JobDescription.Trim(); entity.Requirements = request.Requirements.Trim(); entity.PublicationDate = request.PublicationDate; entity.ClosingDate = request.ClosingDate; entity.Status = request.Status;
        AddAudit("UpdateJobOpening", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<JobOpeningDto> PublishVacancyAsync(Guid id, PublishVacancyRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var entity = await dbContext.JobOpenings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Job opening was not found.");
        entity.Status = "Published"; entity.PublicationDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        dbContext.VacancyPublications.Add(new VacancyPublication { JobOpeningId = id, Channel = request.Channel, ExpiresAt = request.ExpiresAt });
        AddAudit("PublishVacancy", id); await dbContext.SaveChangesAsync(cancellationToken); return Map(entity);
    }

    public async Task<ApplicationDto> CreateApplicationAsync(ApplicationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.JobOpenings.AnyAsync(x => x.Id == request.JobOpeningId, cancellationToken)) throw new BusinessRuleException("Job opening is invalid.");
        var email = request.Email.Trim().ToLowerInvariant();
        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (candidate is null)
        {
            candidate = new Candidate { Name = request.CandidateName.Trim(), Email = email, Phone = request.Phone, Source = request.Source };
            dbContext.Candidates.Add(candidate);
        }
        if (await dbContext.CandidateApplications.AnyAsync(x => x.CandidateId == candidate.Id && x.JobOpeningId == request.JobOpeningId, cancellationToken)) throw new BusinessRuleException("Candidate has already applied for this job.");
        var application = new CandidateApplication { CandidateId = candidate.Id, JobOpeningId = request.JobOpeningId, Status = "Applied" };
        dbContext.CandidateApplications.Add(application);
        AddStatus(application.Id, null, "Applied", "Application received");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapApplicationAsync(application.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> ListApplicationsAsync(string? status, CancellationToken cancellationToken = default)
    {
        EnsureView(); var source = dbContext.CandidateApplications.AsNoTracking().Include(x => x.Candidate).AsQueryable(); if (!string.IsNullOrWhiteSpace(status)) source = source.Where(x => x.Status == status); var rows = await source.OrderByDescending(x => x.AppliedAt).ToListAsync(cancellationToken); return rows.Select(Map).ToList();
    }

    public async Task<ApplicationDto> GetApplicationAsync(Guid id, CancellationToken cancellationToken = default) { EnsureView(); return await MapApplicationAsync(id, cancellationToken); }
    public async Task<ApplicationDto> ShortlistAsync(Guid id, CancellationToken cancellationToken = default) => await UpdateStatusCoreAsync(id, "Shortlisted", "Candidate shortlisted", cancellationToken);

    public async Task<object> ScheduleInterviewAsync(InterviewScheduleRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var application = await dbContext.CandidateApplications.FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken) ?? throw new NotFoundException("Application was not found.");
        var schedule = new InterviewSchedule { ApplicationId = request.ApplicationId, InterviewStage = request.InterviewStage.Trim(), DateTime = request.DateTime, VenueOrMode = request.VenueOrMode };
        dbContext.InterviewSchedules.Add(schedule);
        foreach (var userId in request.PanelUserIds.Distinct())
            if (await dbContext.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken)) dbContext.InterviewPanelMembers.Add(new InterviewPanelMember { InterviewScheduleId = schedule.Id, UserId = userId });
        application.Status = "Interview Scheduled"; AddStatus(application.Id, "Shortlisted", "Interview Scheduled", "Interview scheduled");
        await dbContext.SaveChangesAsync(cancellationToken); return new { schedule.Id, schedule.DateTime, schedule.Status };
    }

    public async Task<object> SubmitFeedbackAsync(Guid id, InterviewFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        if (!await dbContext.InterviewSchedules.AnyAsync(x => x.Id == id, cancellationToken)) throw new NotFoundException("Interview schedule was not found.");
        dbContext.InterviewFeedbacks.Add(new InterviewFeedback { InterviewScheduleId = id, PanelMemberId = request.PanelMemberId, Score = request.Score, Comments = request.Comments });
        var applicationId = await dbContext.InterviewSchedules.Where(x => x.Id == id).Select(x => x.ApplicationId).FirstAsync(cancellationToken);
        var application = await dbContext.CandidateApplications.FirstAsync(x => x.Id == applicationId, cancellationToken);
        application.Status = "Interviewed"; AddStatus(application.Id, "Interview Scheduled", "Interviewed", "Interview feedback submitted");
        await dbContext.SaveChangesAsync(cancellationToken); return new { InterviewScheduleId = id, request.Score };
    }

    public async Task<ApplicationDto> UpdateStatusAsync(Guid id, CandidateStatusRequest request, CancellationToken cancellationToken = default) => await UpdateStatusCoreAsync(id, request.Status, request.Comments, cancellationToken);

    public async Task<object> GenerateOfferLetterAsync(Guid id, OfferLetterRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var application = await dbContext.CandidateApplications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Application was not found.");
        if (application.Status != "Selected") throw new BusinessRuleException("Candidate must be selected before offer letter.");
        var offer = new OfferLetter { ApplicationId = id, OfferDetails = request.OfferDetails.Trim(), Salary = request.Salary, Status = request.SendNow ? "Sent" : "Draft", SentAt = request.SendNow ? DateTime.UtcNow : null };
        dbContext.OfferLetters.Add(offer); application.Status = "Offer Sent"; AddStatus(id, "Selected", "Offer Sent", "Offer letter generated");
        await dbContext.SaveChangesAsync(cancellationToken); return new { offer.Id, offer.Status };
    }

    public async Task<object> ConvertToEmployeeAsync(Guid id, ConvertCandidateRequest request, CancellationToken cancellationToken = default)
    {
        EnsureManage();
        var application = await dbContext.CandidateApplications.Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Application was not found.");
        if (application.Status != "Offer Sent" && application.Status != "Selected") throw new BusinessRuleException("Hired conversion requires selected candidate or sent offer.");
        var parts = (application.Candidate?.Name ?? "New Employee").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var employee = new Employee { EmployeeNumber = request.EmployeeNumber.Trim(), Status = "Active", WorkEmail = application.Candidate?.Email ?? string.Empty, DepartmentId = request.DepartmentId, JobTitleId = request.JobTitleId, GradeLevelId = request.GradeLevelId };
        dbContext.Employees.Add(employee);
        dbContext.EmployeePersonalInfos.Add(new EmployeePersonalInfo { EmployeeId = employee.Id, FirstName = parts.ElementAtOrDefault(0) ?? "New", LastName = parts.ElementAtOrDefault(1) ?? "Employee", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), Gender = "Unspecified" });
        dbContext.EmployeeContactInfos.Add(new EmployeeContactInfo { EmployeeId = employee.Id, WorkEmail = employee.WorkEmail, Phone = application.Candidate?.Phone });
        dbContext.EmployeeEmploymentInfos.Add(new EmployeeEmploymentInfo { EmployeeId = employee.Id, DepartmentId = request.DepartmentId, JobTitleId = request.JobTitleId, GradeLevelId = request.GradeLevelId, HireDate = request.HireDate });
        application.Status = "Hired"; AddStatus(id, "Offer Sent", "Hired", "Candidate converted to employee");
        await dbContext.SaveChangesAsync(cancellationToken); return new { EmployeeId = employee.Id, application.Status };
    }

    private async Task<ApplicationDto> UpdateStatusCoreAsync(Guid id, string status, string? comments, CancellationToken ct)
    {
        EnsureManage(); if (!Pipeline.Contains(status)) throw new BusinessRuleException("Candidate status is invalid.");
        var application = await dbContext.CandidateApplications.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Application was not found.");
        if (application.Status == "Hired") throw new BusinessRuleException("Hired applications cannot transition.");
        var oldStatus = application.Status; application.Status = status; AddStatus(id, oldStatus, status, comments);
        await dbContext.SaveChangesAsync(ct); return await MapApplicationAsync(id, ct);
    }

    private void AddStatus(Guid applicationId, string? oldStatus, string newStatus, string? comments) => dbContext.RecruitmentStatusHistories.Add(new RecruitmentStatusHistory { ApplicationId = applicationId, OldStatus = oldStatus, NewStatus = newStatus, ChangedBy = CurrentUserGuid(), Comments = comments });
    private async Task<ApplicationDto> MapApplicationAsync(Guid id, CancellationToken ct) => Map(await dbContext.CandidateApplications.AsNoTracking().Include(x => x.Candidate).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Application was not found."));
    private static ApplicationDto Map(CandidateApplication x) => new(x.Id, x.CandidateId, x.Candidate?.Name ?? string.Empty, x.JobOpeningId, x.Status, x.AppliedAt);
    private static JobOpeningDto Map(JobOpening x) => new(x.Id, x.Title, x.DepartmentId, x.Vacancies, x.Status, x.ClosingDate);
    private static void ValidateJob(JobOpeningRequest request) { if (request.Vacancies <= 0) throw new BusinessRuleException("Vacancies must be positive."); if (request.PublicationDate is not null && request.ClosingDate < request.PublicationDate) throw new BusinessRuleException("Closing date cannot be before publication date."); }
    private void AddAudit(string action, Guid id) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = "Recruitment", Action = action, EntityType = "Recruitment", EntityId = id.ToString(), Outcome = "Success" });
    private void EnsureView() { if (!currentUser.Permissions.Contains("Recruitment.View") && !currentUser.Permissions.Contains("Recruitment.Manage")) throw new ForbiddenException("Recruitment view permission is required."); }
    private void EnsureManage() { if (!currentUser.Permissions.Contains("Recruitment.Manage")) throw new ForbiddenException("Recruitment management permission is required."); }
    private Guid CurrentUserGuid() => Guid.TryParse(currentUser.UserId, out var id) ? id : throw new ForbiddenException("Authenticated user id is invalid.");
}
