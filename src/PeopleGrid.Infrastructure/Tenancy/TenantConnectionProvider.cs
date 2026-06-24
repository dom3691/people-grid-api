using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Infrastructure.Persistence;

namespace PeopleGrid.Infrastructure.Tenancy;

public sealed class TenantConnectionProvider(ICurrentTenantService currentTenant, PlatformDbContext platformDbContext) : ITenantConnectionProvider
{
    public string GetTenantConnectionString()
    {
        if (string.IsNullOrWhiteSpace(currentTenant.ConnectionString))
        {
            throw new InvalidOperationException("Tenant connection string has not been resolved for this request.");
        }

        return currentTenant.ConnectionString;
    }

    public async Task<string?> GetConnectionStringByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        var normalized = tenantCode.Trim().ToUpperInvariant();
        return await platformDbContext.Tenants
            .Where(x => x.Code == normalized && x.IsActive)
            .Select(x => x.ConnectionString)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
