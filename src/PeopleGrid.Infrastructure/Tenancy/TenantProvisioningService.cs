using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Seeders;

namespace PeopleGrid.Infrastructure.Tenancy;

public sealed class TenantProvisioningService(
    ILogger<TenantProvisioningService> logger,
    IApplicationDbContext dbContext,
    RolePermissionSeeder seeder) : ITenantProvisioningService
{
    public async Task ProvisionTenantDatabaseAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Provision tenant database placeholder for {TenantCode} -> {DatabaseName}", tenant.Code, tenant.DatabaseName);
        if (dbContext is DbContext efContext)
        {
            await efContext.Database.MigrateAsync(cancellationToken);
        }

        await seeder.SeedAsync(dbContext, cancellationToken);
    }
}
