namespace PeopleGrid.Application.Features.Training.DTOs;

public sealed record TrainingProgramRequest(string Title, string? Description, string? ProviderId, string? Venue, DateOnly StartDate, DateOnly EndDate, decimal Cost, int Capacity, Guid? TargetDepartmentId, Guid? TargetGradeLevelId, Guid? TargetSkillId, string Status = "Draft");
public sealed record TrainingProgramDto(Guid Id, string Title, DateOnly StartDate, DateOnly EndDate, decimal Cost, int Capacity, string Status);
public sealed record TrainingNominationRequest(IReadOnlyCollection<Guid> EmployeeIds);
public sealed record TrainingDecisionRequest(string? Comments);
public sealed record TrainingAttendanceRequest(Guid EmployeeId, DateOnly SessionDate, bool Attended);
public sealed record TrainingFeedbackRequest(Guid EmployeeId, int Score, string? Comments);
public sealed record TrainingCertificateRequest(Guid EmployeeId, string FileName, string StorageKey, DateOnly IssuedDate);
public sealed record TrainingHistoryDto(Guid EmployeeId, Guid ProgramId, string Status, DateTime? CompletedAt);
public sealed record EmployeeSkillDto(Guid EmployeeId, Guid SkillId, string SkillName, string ProficiencyLevel, DateOnly? AcquiredDate, string Source);

