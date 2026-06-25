namespace PeopleGrid.Application.Features.Recruitment.DTOs;

public sealed record JobOpeningRequest(string Title, Guid DepartmentId, Guid? BranchId, Guid? HiringManagerId, int Vacancies, string EmploymentType, Guid? GradeLevelId, string JobDescription, string Requirements, DateOnly? PublicationDate, DateOnly ClosingDate, string Status = "Draft");
public sealed record JobOpeningDto(Guid Id, string Title, Guid DepartmentId, int Vacancies, string Status, DateOnly ClosingDate);
public sealed record PublishVacancyRequest(string Channel, DateTime ExpiresAt);
public sealed record ApplicationRequest(Guid JobOpeningId, string CandidateName, string Email, string? Phone, string? Source);
public sealed record ApplicationDto(Guid Id, Guid CandidateId, string CandidateName, Guid JobOpeningId, string Status, DateTime AppliedAt);
public sealed record InterviewScheduleRequest(Guid ApplicationId, string InterviewStage, DateTime DateTime, string? VenueOrMode, IReadOnlyCollection<Guid> PanelUserIds);
public sealed record InterviewFeedbackRequest(Guid PanelMemberId, decimal Score, string? Comments);
public sealed record CandidateStatusRequest(string Status, string? Comments);
public sealed record OfferLetterRequest(string OfferDetails, decimal Salary, bool SendNow = true);
public sealed record ConvertCandidateRequest(string EmployeeNumber, Guid? DepartmentId, Guid? JobTitleId, Guid? GradeLevelId, DateOnly HireDate);

