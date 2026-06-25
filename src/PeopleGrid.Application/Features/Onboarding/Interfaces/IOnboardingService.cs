using PeopleGrid.Application.Features.Onboarding.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Onboarding.Interfaces;

public interface IOnboardingService
{
    Task<OnboardingPlanDto> CreatePlanAsync(CreateOnboardingPlanRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<OnboardingPlanDto>> ListPlansAsync(OnboardingPlanQuery query, CancellationToken cancellationToken = default);
    Task<OnboardingPlanDto> GetPlanAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OnboardingPlanDto> UpdatePlanAsync(Guid id, UpdateOnboardingPlanRequest request, CancellationToken cancellationToken = default);
    Task<OnboardingTaskDto> AddTaskAsync(Guid planId, AddOnboardingTaskRequest request, CancellationToken cancellationToken = default);
    Task<OnboardingTaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<OnboardingTaskDto> ReopenTaskAsync(Guid taskId, ReopenTaskRequest request, CancellationToken cancellationToken = default);
    Task SubmitDocumentAsync(Guid planId, Guid? documentTypeId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(Guid planId, CancellationToken cancellationToken = default);
    Task AcknowledgePolicyAsync(Guid planId, AcknowledgePolicyRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<OnboardingProgressDto> GetProgressAsync(Guid planId, CancellationToken cancellationToken = default);
}
