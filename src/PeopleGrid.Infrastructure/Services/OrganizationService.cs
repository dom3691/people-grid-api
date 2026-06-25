using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Organization.DTOs;
using PeopleGrid.Application.Features.Organization.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class OrganizationService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IOrganizationService
{
    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureDepartmentCodeUniqueAsync(null, code, cancellationToken);
        await ValidateActiveUserAsync(request.HeadUserId, "Selected department head is invalid.", cancellationToken);

        var department = new Department
        {
            Code = code,
            Name = request.Name.Trim(),
            HeadUserId = request.HeadUserId,
            Status = request.Status,
            IsActive = request.Status == "Active"
        };

        dbContext.Departments.Add(department);
        AddAudit("Organization", "CreateDepartment", "Department", department.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDepartmentAsync(department.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<DepartmentDto>> ListDepartmentsAsync(OrganizationListQuery query, CancellationToken cancellationToken = default)
    {
        var departments = DepartmentQuery();
        departments = ApplyOrganizationFilters(departments, query.Search, query.Status);
        departments = ApplyDepartmentSorting(departments, query.SortBy, query.SortDirection);

        var totalCount = await departments.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await departments.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(MapDepartment).ToList();
        return new PaginatedResponse<DepartmentDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await DepartmentQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Department was not found.");

        return MapDepartment(department);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = await dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Department was not found.");

        var code = NormalizeCode(request.Code);
        await EnsureDepartmentCodeUniqueAsync(id, code, cancellationToken);
        await ValidateActiveUserAsync(request.HeadUserId, "Selected department head is invalid.", cancellationToken);
        await EnsureCanUseStatusAsync(request.Status, () => HasActiveUsersInDepartmentAsync(id, cancellationToken), "Department cannot be deactivated while active users are assigned to it.");

        department.Code = code;
        department.Name = request.Name.Trim();
        department.HeadUserId = request.HeadUserId;
        department.Status = request.Status;
        department.IsActive = request.Status == "Active";

        AddAudit("Organization", "UpdateDepartment", "Department", department.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDepartmentAsync(department.Id, cancellationToken);
    }

    public async Task<DepartmentDto> UpdateDepartmentStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken cancellationToken = default)
    {
        var department = await dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Department was not found.");

        await EnsureCanUseStatusAsync(request.Status, () => HasActiveUsersInDepartmentAsync(id, cancellationToken), "Department cannot be deactivated while active users are assigned to it.");
        department.Status = request.Status;
        department.IsActive = request.Status == "Active";

        AddAudit("Organization", "UpdateDepartmentStatus", "Department", department.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDepartmentAsync(department.Id, cancellationToken);
    }

    public async Task<UnitDto> CreateUnitAsync(CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateActiveDepartmentAsync(request.DepartmentId, cancellationToken);
        var code = NormalizeCode(request.Code);
        await EnsureUnitCodeUniqueAsync(null, code, cancellationToken);

        var unit = new Unit
        {
            DepartmentId = request.DepartmentId,
            Code = code,
            Name = request.Name.Trim(),
            Status = request.Status,
            IsActive = request.Status == "Active"
        };

        dbContext.Units.Add(unit);
        AddAudit("Organization", "CreateUnit", "Unit", unit.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadUnitDtoAsync(unit.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<UnitDto>> ListUnitsAsync(UnitListQuery query, CancellationToken cancellationToken = default)
    {
        var units = UnitQuery();
        units = ApplyOrganizationFilters(units, query.Search, query.Status);
        if (query.DepartmentId is not null)
        {
            units = units.Where(x => x.DepartmentId == query.DepartmentId);
        }

        units = ApplyUnitSorting(units, query.SortBy, query.SortDirection);
        var totalCount = await units.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await units.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(MapUnit).ToList();
        return new PaginatedResponse<UnitDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<UnitDto> UpdateUnitAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Unit was not found.");

        await ValidateActiveDepartmentAsync(request.DepartmentId, cancellationToken);
        var code = NormalizeCode(request.Code);
        await EnsureUnitCodeUniqueAsync(id, code, cancellationToken);
        await EnsureCanUseStatusAsync(request.Status, () => HasActiveUsersInUnitAsync(id, cancellationToken), "Unit cannot be deactivated while active users are assigned to it.");

        unit.DepartmentId = request.DepartmentId;
        unit.Code = code;
        unit.Name = request.Name.Trim();
        unit.Status = request.Status;
        unit.IsActive = request.Status == "Active";

        AddAudit("Organization", "UpdateUnit", "Unit", unit.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadUnitDtoAsync(unit.Id, cancellationToken);
    }

    public async Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureBranchCodeUniqueAsync(null, code, cancellationToken);

        var branch = new Branch
        {
            Code = code,
            Name = request.Name.Trim(),
            Address = request.Address?.Trim(),
            Country = request.Country?.Trim(),
            StateRegion = request.StateRegion?.Trim(),
            Status = request.Status,
            IsActive = request.Status == "Active"
        };

        dbContext.Branches.Add(branch);
        AddAudit("Organization", "CreateBranch", "Branch", branch.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadBranchDtoAsync(branch.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<BranchDto>> ListBranchesAsync(OrganizationListQuery query, CancellationToken cancellationToken = default)
    {
        var branches = BranchQuery();
        branches = ApplyOrganizationFilters(branches, query.Search, query.Status);
        branches = ApplyBranchSorting(branches, query.SortBy, query.SortDirection);

        var totalCount = await branches.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await branches.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(MapBranch).ToList();
        return new PaginatedResponse<BranchDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<BranchDto> UpdateBranchAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Branch was not found.");

        var code = NormalizeCode(request.Code);
        await EnsureBranchCodeUniqueAsync(id, code, cancellationToken);
        await EnsureCanUseStatusAsync(request.Status, () => HasActiveUsersInBranchAsync(id, cancellationToken), "Branch cannot be deactivated while active users are assigned to it.");

        branch.Code = code;
        branch.Name = request.Name.Trim();
        branch.Address = request.Address?.Trim();
        branch.Country = request.Country?.Trim();
        branch.StateRegion = request.StateRegion?.Trim();
        branch.Status = request.Status;
        branch.IsActive = request.Status == "Active";

        AddAudit("Organization", "UpdateBranch", "Branch", branch.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadBranchDtoAsync(branch.Id, cancellationToken);
    }

    public async Task<JobTitleDto> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureJobTitleCodeUniqueAsync(null, code, cancellationToken);
        await ValidateGradeLevelAsync(request.GradeLevelId, cancellationToken);

        var jobTitle = new JobTitle
        {
            Code = code,
            Name = request.Name.Trim(),
            GradeLevelId = request.GradeLevelId,
            Status = request.Status,
            IsActive = request.Status == "Active"
        };

        dbContext.JobTitles.Add(jobTitle);
        AddAudit("Organization", "CreateJobTitle", "JobTitle", jobTitle.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadJobTitleDtoAsync(jobTitle.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<JobTitleDto>> ListJobTitlesAsync(OrganizationListQuery query, CancellationToken cancellationToken = default)
    {
        var jobTitles = JobTitleQuery();
        jobTitles = ApplyOrganizationFilters(jobTitles, query.Search, query.Status);
        jobTitles = ApplyJobTitleSorting(jobTitles, query.SortBy, query.SortDirection);

        var totalCount = await jobTitles.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await jobTitles.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(MapJobTitle).ToList();
        return new PaginatedResponse<JobTitleDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<JobTitleDto> UpdateJobTitleAsync(Guid id, UpdateJobTitleRequest request, CancellationToken cancellationToken = default)
    {
        var jobTitle = await dbContext.JobTitles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Job title was not found.");

        var code = NormalizeCode(request.Code);
        await EnsureJobTitleCodeUniqueAsync(id, code, cancellationToken);
        await ValidateGradeLevelAsync(request.GradeLevelId, cancellationToken);
        await EnsureCanUseStatusAsync(request.Status, () => HasActiveUsersInJobTitleAsync(id, cancellationToken), "Job title cannot be deactivated while active users are assigned to it.");

        jobTitle.Code = code;
        jobTitle.Name = request.Name.Trim();
        jobTitle.GradeLevelId = request.GradeLevelId;
        jobTitle.Status = request.Status;
        jobTitle.IsActive = request.Status == "Active";

        AddAudit("Organization", "UpdateJobTitle", "JobTitle", jobTitle.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadJobTitleDtoAsync(jobTitle.Id, cancellationToken);
    }

    public async Task<ManagerAssignmentDto> AssignManagerAsync(AssignManagerRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateActiveUserAsync(request.UserId, "Selected user is invalid.", cancellationToken);
        await ValidateActiveUserAsync(request.ManagerUserId, "Selected manager is invalid.", cancellationToken);
        await EnsureNoCircularManagerAssignmentAsync(request.UserId, request.ManagerUserId, cancellationToken);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var currentAssignments = await dbContext.UserManagerAssignments
            .Where(x => x.UserId == request.UserId && x.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var assignment in currentAssignments)
        {
            assignment.IsCurrent = false;
            assignment.EffectiveTo = request.EffectiveFrom <= assignment.EffectiveFrom ? now : request.EffectiveFrom;
        }

        var newAssignment = new UserManagerAssignment
        {
            UserId = request.UserId,
            ManagerUserId = request.ManagerUserId,
            EffectiveFrom = request.EffectiveFrom,
            IsCurrent = true
        };

        dbContext.UserManagerAssignments.Add(newAssignment);
        AddAudit("Organization", "AssignManager", "UserManagerAssignment", newAssignment.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await LoadManagerAssignmentDtoAsync(newAssignment.Id, cancellationToken);
    }

    private IQueryable<Department> DepartmentQuery()
    {
        return dbContext.Departments
            .Include(x => x.HeadUser)
            .Include(x => x.Units)
            .Include(x => x.UserProfiles).ThenInclude(x => x.User);
    }

    private IQueryable<Unit> UnitQuery()
    {
        return dbContext.Units
            .Include(x => x.Department)
            .Include(x => x.UserProfiles).ThenInclude(x => x.User);
    }

    private IQueryable<Branch> BranchQuery()
    {
        return dbContext.Branches.Include(x => x.UserProfiles).ThenInclude(x => x.User);
    }

    private IQueryable<JobTitle> JobTitleQuery()
    {
        return dbContext.JobTitles
            .Include(x => x.GradeLevel)
            .Include(x => x.UserProfiles).ThenInclude(x => x.User);
    }

    private async Task<UnitDto> LoadUnitDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var unit = await UnitQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Unit was not found.");
        return MapUnit(unit);
    }

    private async Task<BranchDto> LoadBranchDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var branch = await BranchQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Branch was not found.");
        return MapBranch(branch);
    }

    private async Task<JobTitleDto> LoadJobTitleDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var jobTitle = await JobTitleQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Job title was not found.");
        return MapJobTitle(jobTitle);
    }

    private async Task<ManagerAssignmentDto> LoadManagerAssignmentDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.UserManagerAssignments
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.ManagerUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Manager assignment was not found.");

        return new ManagerAssignmentDto(
            assignment.Id,
            assignment.UserId,
            FullName(assignment.User),
            assignment.ManagerUserId,
            FullName(assignment.ManagerUser),
            assignment.EffectiveFrom,
            assignment.EffectiveTo,
            assignment.IsCurrent);
    }

    private static DepartmentDto MapDepartment(Department department)
    {
        return new DepartmentDto(
            department.Id,
            department.Code,
            department.Name,
            department.HeadUserId,
            department.HeadUser is null ? null : FullName(department.HeadUser),
            department.Status,
            department.IsActive,
            department.Units.Count(x => x.IsActive),
            department.UserProfiles.Count(x => x.User != null && x.User.IsActive),
            department.CreatedAt,
            department.UpdatedAt);
    }

    private static UnitDto MapUnit(Unit unit)
    {
        return new UnitDto(
            unit.Id,
            unit.DepartmentId,
            unit.Department?.Name ?? string.Empty,
            unit.Code,
            unit.Name,
            unit.Status,
            unit.IsActive,
            unit.UserProfiles.Count(x => x.User != null && x.User.IsActive),
            unit.CreatedAt,
            unit.UpdatedAt);
    }

    private static BranchDto MapBranch(Branch branch)
    {
        return new BranchDto(
            branch.Id,
            branch.Code,
            branch.Name,
            branch.Address,
            branch.Country,
            branch.StateRegion,
            branch.Status,
            branch.IsActive,
            branch.UserProfiles.Count(x => x.User != null && x.User.IsActive),
            branch.CreatedAt,
            branch.UpdatedAt);
    }

    private static JobTitleDto MapJobTitle(JobTitle jobTitle)
    {
        return new JobTitleDto(
            jobTitle.Id,
            jobTitle.Code,
            jobTitle.Name,
            jobTitle.GradeLevelId,
            jobTitle.GradeLevel?.Name,
            jobTitle.Status,
            jobTitle.IsActive,
            jobTitle.UserProfiles.Count(x => x.User != null && x.User.IsActive),
            jobTitle.CreatedAt,
            jobTitle.UpdatedAt);
    }

    private static IQueryable<T> ApplyOrganizationFilters<T>(IQueryable<T> query, string? search, string? status)
        where T : class
    {
        if (typeof(T) == typeof(Department))
        {
            var departments = (IQueryable<Department>)query;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim().ToLowerInvariant();
                departments = departments.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                departments = departments.Where(x => x.Status == status);
            }

            return (IQueryable<T>)departments;
        }

        if (typeof(T) == typeof(Unit))
        {
            var units = (IQueryable<Unit>)query;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim().ToLowerInvariant();
                units = units.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                units = units.Where(x => x.Status == status);
            }

            return (IQueryable<T>)units;
        }

        if (typeof(T) == typeof(Branch))
        {
            var branches = (IQueryable<Branch>)query;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim().ToLowerInvariant();
                branches = branches.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                branches = branches.Where(x => x.Status == status);
            }

            return (IQueryable<T>)branches;
        }

        var jobTitles = (IQueryable<JobTitle>)query;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            jobTitles = jobTitles.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            jobTitles = jobTitles.Where(x => x.Status == status);
        }

        return (IQueryable<T>)jobTitles;
    }

    private static IQueryable<Department> ApplyDepartmentSorting(IQueryable<Department> query, string? sortBy, string? sortDirection)
    {
        var descending = IsDescending(sortDirection);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private static IQueryable<Unit> ApplyUnitSorting(IQueryable<Unit> query, string? sortBy, string? sortDirection)
    {
        var descending = IsDescending(sortDirection);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private static IQueryable<Branch> ApplyBranchSorting(IQueryable<Branch> query, string? sortBy, string? sortDirection)
    {
        var descending = IsDescending(sortDirection);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private static IQueryable<JobTitle> ApplyJobTitleSorting(IQueryable<JobTitle> query, string? sortBy, string? sortDirection)
    {
        var descending = IsDescending(sortDirection);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private async Task EnsureDepartmentCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.Departments.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Department code already exists.");
        }
    }

    private async Task EnsureUnitCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.Units.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Unit code already exists.");
        }
    }

    private async Task EnsureBranchCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.Branches.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Branch code already exists.");
        }
    }

    private async Task EnsureJobTitleCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.JobTitles.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Job title code already exists.");
        }
    }

    private async Task ValidateActiveDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive, cancellationToken);
        if (!exists)
        {
            throw new BusinessRuleException("Selected department is invalid.");
        }
    }

    private async Task ValidateGradeLevelAsync(Guid? gradeLevelId, CancellationToken cancellationToken)
    {
        if (gradeLevelId is not null && !await dbContext.GradeLevels.AnyAsync(x => x.Id == gradeLevelId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected grade level is invalid.");
        }
    }

    private async Task ValidateActiveUserAsync(Guid? userId, string message, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return;
        }

        if (!await dbContext.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException(message);
        }
    }

    private async Task EnsureCanUseStatusAsync(string status, Func<Task<bool>> activeUseCheck, string message)
    {
        if (status == "Inactive" && await activeUseCheck())
        {
            throw new BusinessRuleException(message);
        }
    }

    private Task<bool> HasActiveUsersInDepartmentAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles.AnyAsync(x => x.DepartmentId == id && x.User != null && x.User.IsActive, cancellationToken);
    }

    private Task<bool> HasActiveUsersInUnitAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles.AnyAsync(x => x.UnitId == id && x.User != null && x.User.IsActive, cancellationToken);
    }

    private Task<bool> HasActiveUsersInBranchAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles.AnyAsync(x => x.BranchId == id && x.User != null && x.User.IsActive, cancellationToken);
    }

    private Task<bool> HasActiveUsersInJobTitleAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.UserProfiles.AnyAsync(x => x.JobTitleId == id && x.User != null && x.User.IsActive, cancellationToken);
    }

    private async Task EnsureNoCircularManagerAssignmentAsync(Guid userId, Guid managerUserId, CancellationToken cancellationToken)
    {
        if (userId == managerUserId)
        {
            throw new BusinessRuleException("A user cannot be assigned as their own manager.");
        }

        var visited = new HashSet<Guid> { userId };
        var currentUserId = managerUserId;
        while (true)
        {
            if (!visited.Add(currentUserId))
            {
                throw new BusinessRuleException("Circular manager assignment detected.");
            }

            var nextManagerId = await dbContext.UserManagerAssignments
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId && x.IsCurrent)
                .Select(x => (Guid?)x.ManagerUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextManagerId is null)
            {
                return;
            }

            currentUserId = nextManagerId.Value;
        }
    }

    private void AddAudit(string module, string action, string entityType, string? entityId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Module = module,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = "Success"
        });
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return dbContext is DbContext efContext
            ? await efContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static bool IsDescending(string? sortDirection) => string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    private static string FullName(User? user) => user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
}
