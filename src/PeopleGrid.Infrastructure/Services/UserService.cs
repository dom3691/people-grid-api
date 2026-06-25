using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Users.DTOs;
using PeopleGrid.Application.Features.Users.Interfaces;
using PeopleGrid.Application.Security;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Security;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class UserService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IUserService
{
    public async Task<UserDetailsDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUserUniqueAsync(null, request.EmployeeNumber, request.Email, request.UserName, cancellationToken);
        await ValidateReferencesAsync(request.DepartmentId, request.UnitId, request.BranchId, request.JobTitleId, request.EmploymentTypeId, cancellationToken);

        var roles = await LoadRolesAsync(request.RoleIds, cancellationToken);
        await using var transaction = await BeginTransactionAsync(cancellationToken);

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password) ? GenerateTemporaryPassword() : request.Password;
        var now = DateTime.UtcNow;
        var user = new User
        {
            EmployeeNumber = request.EmployeeNumber.Trim(),
            Email = NormalizeEmail(request.Email),
            UserName = NormalizeUserName(request.UserName),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = PasswordHasher.Hash(temporaryPassword),
            PasswordChangedAt = now,
            Status = "Active",
            IsActive = true
        };

        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            DepartmentId = request.DepartmentId,
            UnitId = request.UnitId,
            BranchId = request.BranchId,
            JobTitleId = request.JobTitleId,
            EmploymentTypeId = request.EmploymentTypeId
        });

        foreach (var role in roles)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedBy = currentUser.UserId,
                AssignedAt = now
            });
        }

        await AddAuditAsync("Users", "Create", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Include(x => x.Profile!).ThenInclude(x => x.Department)
            .Include(x => x.Profile!).ThenInclude(x => x.Branch)
            .Include(x => x.Profile!).ThenInclude(x => x.EmploymentType)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(x =>
                x.EmployeeNumber.ToLower().Contains(search) ||
                x.Email.ToLower().Contains(search) ||
                x.UserName.ToLower().Contains(search) ||
                x.FirstName.ToLower().Contains(search) ||
                x.LastName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            users = users.Where(x => x.Status == query.Status);
        }

        if (query.DepartmentId is not null)
        {
            users = users.Where(x => x.Profile != null && x.Profile.DepartmentId == query.DepartmentId);
        }

        if (query.BranchId is not null)
        {
            users = users.Where(x => x.Profile != null && x.Profile.BranchId == query.BranchId);
        }

        if (query.EmploymentTypeId is not null)
        {
            users = users.Where(x => x.Profile != null && x.Profile.EmploymentTypeId == query.EmploymentTypeId);
        }

        if (query.RoleId is not null)
        {
            users = users.Where(x => x.UserRoles.Any(ur => ur.RoleId == query.RoleId));
        }

        users = ApplySorting(users, query.SortBy, query.SortDirection);

        var totalCount = await users.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var items = await users
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(x => new UserListItemDto(
                x.Id,
                x.EmployeeNumber,
                x.Email,
                x.UserName,
                (x.FirstName + " " + x.LastName).Trim(),
                x.Status,
                x.Profile != null && x.Profile.Department != null ? x.Profile.Department.Name : null,
                x.Profile != null && x.Profile.Branch != null ? x.Profile.Branch.Name : null,
                x.Profile != null && x.Profile.EmploymentType != null ? x.Profile.EmploymentType.Name : null,
                x.UserRoles.Select(ur => ur.Role!.Name).ToArray(),
                x.CreatedAt,
                x.LastLoginAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<UserListItemDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<UserDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        return MapToDetails(user);
    }

    public async Task<UserDetailsDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUserUniqueAsync(id, request.EmployeeNumber, request.Email, request.UserName, cancellationToken);
        await ValidateReferencesAsync(request.DepartmentId, request.UnitId, request.BranchId, request.JobTitleId, request.EmploymentTypeId, cancellationToken);

        var user = await dbContext.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.EmployeeNumber = request.EmployeeNumber.Trim();
        user.Email = NormalizeEmail(request.Email);
        user.UserName = NormalizeUserName(request.UserName);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Status = request.Status;
        user.IsActive = request.Status == "Active";

        user.Profile ??= new UserProfile { UserId = user.Id };
        user.Profile.FirstName = request.FirstName.Trim();
        user.Profile.LastName = request.LastName.Trim();
        user.Profile.Phone = request.Phone?.Trim();
        user.Profile.DepartmentId = request.DepartmentId;
        user.Profile.UnitId = request.UnitId;
        user.Profile.BranchId = request.BranchId;
        user.Profile.JobTitleId = request.JobTitleId;
        user.Profile.EmploymentTypeId = request.EmploymentTypeId;

        await AddAuditAsync("Users", "Update", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.IsActive = true;
        user.Status = "Active";
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        await AddAuditAsync("Users", "Activate", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (await IsLastActiveSuperAdminAsync(user, cancellationToken))
        {
            throw new BusinessRuleException("The last active Super Admin cannot be deactivated.");
        }

        user.IsActive = false;
        user.Status = "Deactivated";
        await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);

        await AddAuditAsync("Users", "Deactivate", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDetailsDto> AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var newRoles = await LoadRolesAsync(request.RoleIds, cancellationToken);
        var willRemoveSuperAdmin = user.UserRoles.Any(x => x.Role!.Name == RoleConstants.SuperAdmin) &&
                                   newRoles.All(x => x.Name != RoleConstants.SuperAdmin);
        if (willRemoveSuperAdmin && await IsLastActiveSuperAdminAsync(user, cancellationToken))
        {
            throw new BusinessRuleException("The last active Super Admin cannot lose the Super Admin role.");
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);

        dbContext.UserRoles.RemoveRange(user.UserRoles);
        foreach (var role in newRoles)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedBy = currentUser.UserId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await AddAuditAsync("Users", "AssignRoles", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminResetPasswordResponse> ResetPasswordAsync(Guid id, AdminResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        var temporaryPassword = string.IsNullOrWhiteSpace(request.NewPassword) ? GenerateTemporaryPassword() : request.NewPassword;
        user.PasswordHash = PasswordHasher.Hash(temporaryPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);
        await AddAuditAsync("Users", "AdminResetPassword", "User", user.Id.ToString(), "Success", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminResetPasswordResponse(temporaryPassword);
    }

    public async Task<UserLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await dbContext.Departments.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var units = await dbContext.Units.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var branches = await dbContext.Branches.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var jobTitles = await dbContext.JobTitles.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var employmentTypes = await dbContext.EmploymentTypes.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new RoleSummaryDto(x.Id, x.Name, x.Code)).ToListAsync(cancellationToken);

        return new UserLookupsDto(departments, units, branches, jobTitles, employmentTypes, roles);
    }

    private IQueryable<User> LoadUserDetailsQuery()
    {
        return dbContext.Users
            .Include(x => x.Profile!).ThenInclude(x => x.Department)
            .Include(x => x.Profile!).ThenInclude(x => x.Unit)
            .Include(x => x.Profile!).ThenInclude(x => x.Branch)
            .Include(x => x.Profile!).ThenInclude(x => x.JobTitle)
            .Include(x => x.Profile!).ThenInclude(x => x.EmploymentType)
            .Include(x => x.UserRoles).ThenInclude(x => x.Role);
    }

    private static UserDetailsDto MapToDetails(User user)
    {
        return new UserDetailsDto(
            user.Id,
            user.EmployeeNumber,
            user.Email,
            user.UserName,
            user.Profile?.FirstName ?? user.FirstName,
            user.Profile?.LastName ?? user.LastName,
            user.Profile?.Phone,
            user.Status,
            user.IsActive,
            user.LockoutEnd,
            user.FailedLoginCount,
            user.LastLoginAt,
            user.Profile?.DepartmentId,
            user.Profile?.Department?.Name,
            user.Profile?.UnitId,
            user.Profile?.Unit?.Name,
            user.Profile?.BranchId,
            user.Profile?.Branch?.Name,
            user.Profile?.JobTitleId,
            user.Profile?.JobTitle?.Name,
            user.Profile?.EmploymentTypeId,
            user.Profile?.EmploymentType?.Name,
            user.UserRoles.Select(x => new RoleSummaryDto(x.RoleId, x.Role!.Name, x.Role.Code)).ToArray());
    }

    private async Task EnsureUserUniqueAsync(Guid? currentUserId, string employeeNumber, string email, string userName, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedUserName = NormalizeUserName(userName);
        var normalizedEmployeeNumber = employeeNumber.Trim();

        var exists = await dbContext.Users.AnyAsync(x =>
            x.Id != currentUserId &&
            (x.EmployeeNumber == normalizedEmployeeNumber || x.Email == normalizedEmail || x.UserName == normalizedUserName),
            cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException("Employee number, email, or username already exists.");
        }
    }

    private async Task ValidateReferencesAsync(Guid? departmentId, Guid? unitId, Guid? branchId, Guid? jobTitleId, Guid? employmentTypeId, CancellationToken cancellationToken)
    {
        if (departmentId is not null && !await dbContext.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected department is invalid.");
        }

        if (unitId is not null && !await dbContext.Units.AnyAsync(x => x.Id == unitId && x.IsActive && (departmentId == null || x.DepartmentId == departmentId), cancellationToken))
        {
            throw new BusinessRuleException("Selected unit is invalid.");
        }

        if (branchId is not null && !await dbContext.Branches.AnyAsync(x => x.Id == branchId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected branch is invalid.");
        }

        if (jobTitleId is not null && !await dbContext.JobTitles.AnyAsync(x => x.Id == jobTitleId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected job title is invalid.");
        }

        if (employmentTypeId is not null && !await dbContext.EmploymentTypes.AnyAsync(x => x.Id == employmentTypeId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected employment type is invalid.");
        }
    }

    private async Task<List<Role>> LoadRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        var distinctRoleIds = roleIds.Distinct().ToArray();
        var roles = await dbContext.Roles.Where(x => distinctRoleIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken);
        if (roles.Count != distinctRoleIds.Length)
        {
            throw new BusinessRuleException("One or more selected roles are invalid.");
        }

        return roles;
    }

    private async Task<bool> IsLastActiveSuperAdminAsync(User user, CancellationToken cancellationToken)
    {
        var isSuperAdmin = user.UserRoles.Any(x => x.Role?.Name == RoleConstants.SuperAdmin);
        if (!isSuperAdmin)
        {
            return false;
        }

        return await dbContext.Users.CountAsync(x =>
            x.Id != user.Id &&
            x.IsActive &&
            x.UserRoles.Any(ur => ur.Role!.Name == RoleConstants.SuperAdmin),
            cancellationToken) == 0;
    }

    private async Task RevokeUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        var sessions = await dbContext.UserSessions.Where(x => x.UserId == userId && x.IsActive).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
        }
    }

    private async Task AddAuditAsync(string module, string action, string entityType, string? entityId, string outcome, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Module = module,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome
        });
        await Task.CompletedTask;
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return dbContext is DbContext efContext
            ? await efContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> users, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "employeenumber" => descending ? users.OrderByDescending(x => x.EmployeeNumber) : users.OrderBy(x => x.EmployeeNumber),
            "email" => descending ? users.OrderByDescending(x => x.Email) : users.OrderBy(x => x.Email),
            "username" => descending ? users.OrderByDescending(x => x.UserName) : users.OrderBy(x => x.UserName),
            "status" => descending ? users.OrderByDescending(x => x.Status) : users.OrderBy(x => x.Status),
            "name" => descending ? users.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName) : users.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            _ => descending ? users.OrderByDescending(x => x.CreatedAt) : users.OrderBy(x => x.CreatedAt)
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?";
        return new string(RandomNumberGenerator.GetItems(chars.AsSpan(), 14).ToArray());
    }
}
