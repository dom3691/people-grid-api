using PeopleGrid.Application.Features.Organization.DTOs;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Application.Features.Organization.Interfaces;

public interface IOrganizationService
{
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<DepartmentDto>> ListDepartmentsAsync(OrganizationListQuery query, CancellationToken cancellationToken = default);
    Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken cancellationToken = default);

    Task<UnitDto> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<UnitDto>> ListUnitsAsync(UnitListQuery query, CancellationToken cancellationToken = default);
    Task<UnitDto> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);

    Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<BranchDto>> ListBranchesAsync(OrganizationListQuery query, CancellationToken cancellationToken = default);
    Task<BranchDto> UpdateBranchAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default);

    Task<JobTitleDto> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<JobTitleDto>> ListJobTitlesAsync(OrganizationListQuery query, CancellationToken cancellationToken = default);
    Task<JobTitleDto> UpdateJobTitleAsync(Guid id, UpdateJobTitleRequest request, CancellationToken cancellationToken = default);

    Task<ManagerAssignmentDto> AssignManagerAsync(AssignManagerRequest request, CancellationToken cancellationToken = default);
}
