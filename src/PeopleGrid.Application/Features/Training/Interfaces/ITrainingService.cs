using PeopleGrid.Application.Features.Training.DTOs;

namespace PeopleGrid.Application.Features.Training.Interfaces;

public interface ITrainingService
{
    Task<TrainingProgramDto> CreateProgramAsync(TrainingProgramRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainingProgramDto>> ListProgramsAsync(CancellationToken cancellationToken = default);
    Task<TrainingProgramDto> GetProgramAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TrainingProgramDto> UpdateProgramAsync(Guid id, TrainingProgramRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<object>> NominateEmployeesAsync(Guid id, TrainingNominationRequest request, CancellationToken cancellationToken = default);
    Task<object> ApproveNominationAsync(Guid id, TrainingDecisionRequest request, CancellationToken cancellationToken = default);
    Task<object> RejectNominationAsync(Guid id, TrainingDecisionRequest request, CancellationToken cancellationToken = default);
    Task<object> RecordAttendanceAsync(Guid id, TrainingAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitFeedbackAsync(Guid id, TrainingFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<object> UploadCertificateAsync(Guid id, TrainingCertificateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainingHistoryDto>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeSkillDto>> GetSkillsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

