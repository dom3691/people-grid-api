using PeopleGrid.Application.Features.Recruitment.DTOs;

namespace PeopleGrid.Application.Features.Recruitment.Interfaces;

public interface IRecruitmentService
{
    Task<JobOpeningDto> CreateJobOpeningAsync(JobOpeningRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JobOpeningDto>> ListJobOpeningsAsync(CancellationToken cancellationToken = default);
    Task<JobOpeningDto> UpdateJobOpeningAsync(Guid id, JobOpeningRequest request, CancellationToken cancellationToken = default);
    Task<JobOpeningDto> PublishVacancyAsync(Guid id, PublishVacancyRequest request, CancellationToken cancellationToken = default);
    Task<ApplicationDto> CreateApplicationAsync(ApplicationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ApplicationDto>> ListApplicationsAsync(string? status, CancellationToken cancellationToken = default);
    Task<ApplicationDto> GetApplicationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApplicationDto> ShortlistAsync(Guid id, CancellationToken cancellationToken = default);
    Task<object> ScheduleInterviewAsync(InterviewScheduleRequest request, CancellationToken cancellationToken = default);
    Task<object> SubmitFeedbackAsync(Guid id, InterviewFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ApplicationDto> UpdateStatusAsync(Guid id, CandidateStatusRequest request, CancellationToken cancellationToken = default);
    Task<object> GenerateOfferLetterAsync(Guid id, OfferLetterRequest request, CancellationToken cancellationToken = default);
    Task<object> ConvertToEmployeeAsync(Guid id, ConvertCandidateRequest request, CancellationToken cancellationToken = default);
}

