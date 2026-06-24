using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Security;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Persistence;
using PeopleGrid.Infrastructure.Security;

namespace PeopleGrid.Infrastructure.Seeders;

public sealed class RolePermissionSeeder
{
    public async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        foreach (var permissionCode in PermissionConstants.All)
        {
            if (!await dbContext.Permissions.AnyAsync(x => x.Code == permissionCode, cancellationToken))
            {
                var parts = permissionCode.Split('.', 2);
                dbContext.Permissions.Add(new Permission
                {
                    Code = permissionCode,
                    Module = parts[0],
                    Action = parts.Length > 1 ? parts[1] : "Manage",
                    Description = permissionCode
                });
            }
        }

        foreach (var roleName in RoleConstants.DefaultRoles)
        {
            var code = roleName.Replace(" ", string.Empty).ToUpperInvariant();
            if (!await dbContext.Roles.AnyAsync(x => x.Code == code, cancellationToken))
            {
                dbContext.Roles.Add(new Role { Name = roleName, Code = code, IsSystemRole = true });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var superAdmin = await dbContext.Roles.FirstAsync(x => x.Name == RoleConstants.SuperAdmin, cancellationToken);
        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var permission in permissions)
        {
            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == superAdmin.Id && x.PermissionId == permission.Id, cancellationToken))
            {
                dbContext.RolePermissions.Add(new RolePermission { RoleId = superAdmin.Id, PermissionId = permission.Id });
            }
        }

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var user = new User
            {
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@peoplegrid.local",
                UserName = "admin",
                EmployeeNumber = "PG-0001",
                PasswordHash = PasswordHasher.Hash("Admin@12345")
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superAdmin.Id });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
