using PeopleGrid.Application.Features.Employees.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Employees.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDetailsDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> ChangeStatusAsync(Guid id, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, DeactivateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<GenerateEmployeeNumberResponse> GenerateNumberAsync(GenerateEmployeeNumberRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> UpdateEmploymentInfoAsync(Guid id, EmployeeEmploymentInfoRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> UpdateBankInfoAsync(Guid id, EmployeeBankInfoRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto> UpdateNextOfKinAsync(Guid id, EmployeeNextOfKinRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeEmergencyContactDto> UpsertEmergencyContactAsync(Guid id, EmployeeEmergencyContactRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeJobHistoryDto>> GetJobHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
