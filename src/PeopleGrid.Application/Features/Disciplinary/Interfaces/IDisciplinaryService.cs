using PeopleGrid.Application.Features.Disciplinary.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Disciplinary.Interfaces;

public interface IDisciplinaryService
{
    Task<DisciplinaryCaseDto> CreateCaseAsync(CreateDisciplinaryCaseRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<DisciplinaryCaseDto>> ListCasesAsync(DisciplinaryCaseQuery query, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> GetCaseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> RespondAsync(Guid id, DisciplinaryResponseRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> ReviewAsync(Guid id, DisciplinaryReviewRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> IssueWarningAsync(Guid id, WarningLetterRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> RecordSuspensionAsync(Guid id, SuspensionRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> EscalateAsync(Guid id, EscalateDisciplinaryRequest request, CancellationToken cancellationToken = default);
    Task<DisciplinaryCaseDto> CloseAsync(Guid id, CloseDisciplinaryCaseRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DisciplinaryCaseDto>> EmployeeHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
