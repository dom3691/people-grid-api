using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Persistence;
using PeopleGrid.Infrastructure.Seeders;

namespace PeopleGrid.Infrastructure.Tenancy;

public sealed class TenantProvisioningService(
    ILogger<TenantProvisioningService> logger,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    RolePermissionSeeder seeder) : ITenantProvisioningService
{
    public async Task ProvisionTenantDatabaseAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Provision tenant database placeholder for {TenantCode} -> {DatabaseName}", tenant.Code, tenant.DatabaseName);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(db, cancellationToken);
    }
}
