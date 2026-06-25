using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Roles.DTOs;
using PeopleGrid.Application.Features.Roles.Interfaces;
using PeopleGrid.Application.Security;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class RoleService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IRoleService
{
    public async Task<RoleDetailsDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureRoleUniqueAsync(null, code, request.Name, cancellationToken);

        var permissions = await LoadPermissionsAsync(request.PermissionIds ?? [], cancellationToken);
        await using var transaction = await BeginTransactionAsync(cancellationToken);

        var role = new Role
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            IsActive = request.Status == "Active",
            IsSystemRole = false
        };

        dbContext.Roles.Add(role);
        var now = DateTime.UtcNow;
        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                AssignedBy = currentUser.UserId,
                AssignedAt = now
            });
        }

        AddAudit("Roles", "Create", "Role", role.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<RoleListItemDto>> ListAsync(RoleListQuery query, CancellationToken cancellationToken = default)
    {
        var roles = dbContext.Roles
            .AsNoTracking()
            .Include(x => x.UserRoles)
            .Include(x => x.RolePermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            roles = roles.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            roles = roles.Where(x => x.Status == query.Status);
        }

        if (query.IsSystemRole is not null)
        {
            roles = roles.Where(x => x.IsSystemRole == query.IsSystemRole);
        }

        roles = ApplySorting(roles, query.SortBy, query.SortDirection);

        var totalCount = await roles.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var items = await roles
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(x => new RoleListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.IsSystemRole,
                x.Status,
                x.IsActive,
                x.UserRoles.Count(ur => ur.User != null && ur.User.IsActive),
                x.RolePermissions.Count,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RoleListItemDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<RoleDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadRoleDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");

        return MapToDetails(role);
    }

    public async Task<RoleDetailsDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");

        var code = NormalizeCode(request.Code);
        await EnsureRoleUniqueAsync(id, code, request.Name, cancellationToken);

        role.Code = code;
        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        role.Status = request.Status;
        role.IsActive = request.Status == "Active";

        AddAudit("Roles", "Update", "Role", role.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(x => x.UserRoles).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");

        if (role.IsSystemRole)
        {
            throw new BusinessRuleException("System roles cannot be deleted.");
        }

        var assignedToActiveUsers = role.UserRoles.Any(x => x.User is not null && x.User.IsActive);
        if (assignedToActiveUsers)
        {
            throw new BusinessRuleException("Role cannot be deleted while assigned to active users.");
        }

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = currentUser.UserId;
        role.IsActive = false;
        role.Status = "Inactive";

        AddAudit("Roles", "Delete", "Role", role.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PermissionModuleDto>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Action)
            .Select(x => new PermissionDto(x.Id, x.Code, x.Module, x.Action, x.Description))
            .ToListAsync(cancellationToken);

        return GroupPermissions(permissions);
    }

    public async Task<IReadOnlyCollection<PermissionModuleDto>> GetRolePermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Roles.AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Role was not found.");
        }

        var permissions = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == id)
            .OrderBy(x => x.Permission!.Module)
            .ThenBy(x => x.Permission!.Action)
            .Select(x => new PermissionDto(x.PermissionId, x.Permission!.Code, x.Permission.Module, x.Permission.Action, x.Permission.Description))
            .ToListAsync(cancellationToken);

        return GroupPermissions(permissions);
    }

    public async Task<RoleDetailsDto> AssignPermissionsAsync(Guid id, AssignRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Role was not found.");

        if (!role.IsActive)
        {
            throw new BusinessRuleException("Permissions cannot be assigned to an inactive role.");
        }

        var permissions = await LoadPermissionsAsync(request.PermissionIds, cancellationToken);
        await using var transaction = await BeginTransactionAsync(cancellationToken);

        dbContext.RolePermissions.RemoveRange(role.RolePermissions);
        var now = DateTime.UtcNow;
        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                AssignedBy = currentUser.UserId,
                AssignedAt = now
            });
        }

        AddAudit("Roles", "AssignPermissions", "Role", role.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleUserDto>> GetRoleUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Roles.AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Role was not found.");
        }

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleId == id)
            .OrderBy(x => x.User!.FirstName)
            .ThenBy(x => x.User!.LastName)
            .Select(x => new RoleUserDto(
                x.UserId,
                x.User!.EmployeeNumber,
                x.User.Email,
                x.User.UserName,
                (x.User.FirstName + " " + x.User.LastName).Trim(),
                x.User.Status,
                x.AssignedAt,
                x.AssignedBy))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Role> LoadRoleDetailsQuery()
    {
        return dbContext.Roles
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .Include(x => x.UserRoles).ThenInclude(x => x.User);
    }

    private static RoleDetailsDto MapToDetails(Role role)
    {
        var permissions = role.RolePermissions
            .Where(x => x.Permission is not null)
            .OrderBy(x => x.Permission!.Module)
            .ThenBy(x => x.Permission!.Action)
            .Select(x => new PermissionDto(
                x.PermissionId,
                x.Permission!.Code,
                x.Permission.Module,
                x.Permission.Action,
                x.Permission.Description))
            .ToArray();

        return new RoleDetailsDto(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.Status,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt,
            permissions,
            role.UserRoles.Count(x => x.User is not null && x.User.IsActive));
    }

    private async Task EnsureRoleUniqueAsync(Guid? currentRoleId, string code, string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        var exists = await dbContext.Roles.AnyAsync(x =>
            x.Id != currentRoleId &&
            (x.Code == code || x.Name.ToLower() == normalizedName),
            cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException("Role code or name already exists.");
        }
    }

    private async Task<List<Permission>> LoadPermissionsAsync(IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var distinctPermissionIds = permissionIds.Distinct().ToArray();
        var permissions = await dbContext.Permissions
            .Where(x => distinctPermissionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (permissions.Count != distinctPermissionIds.Length)
        {
            throw new BusinessRuleException("One or more selected permissions are invalid.");
        }

        return permissions;
    }

    private static IReadOnlyCollection<PermissionModuleDto> GroupPermissions(IReadOnlyCollection<PermissionDto> permissions)
    {
        return permissions
            .GroupBy(x => x.Module)
            .OrderBy(x => x.Key)
            .Select(x => new PermissionModuleDto(x.Key, x.OrderBy(p => p.Action).ToArray()))
            .ToArray();
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

    private static IQueryable<Role> ApplySorting(IQueryable<Role> roles, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" => descending ? roles.OrderByDescending(x => x.Code) : roles.OrderBy(x => x.Code),
            "name" => descending ? roles.OrderByDescending(x => x.Name) : roles.OrderBy(x => x.Name),
            "status" => descending ? roles.OrderByDescending(x => x.Status) : roles.OrderBy(x => x.Status),
            "issystemrole" => descending ? roles.OrderByDescending(x => x.IsSystemRole) : roles.OrderBy(x => x.IsSystemRole),
            _ => descending ? roles.OrderByDescending(x => x.CreatedAt) : roles.OrderBy(x => x.CreatedAt)
        };
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().Replace(" ", "_").ToUpperInvariant();
    }
}
