using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Infrastructure.Persistence;

namespace PeopleGrid.Infrastructure.Tenancy;

public sealed class TenantResolver(IHttpContextAccessor accessor, PlatformDbContext platformDbContext) : ITenantResolver
{
    public async Task<TenantResolutionResult?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var http = accessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        var tenantCode = http.Request.Headers["X-Tenant-Code"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantCode) && http.User.Identity?.IsAuthenticated == true)
        {
            tenantCode = http.User.Claims.FirstOrDefault(x => x.Type == "TenantCode")?.Value;
        }

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            var host = http.Request.Host.Host;
            var subdomain = host.Split('.').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(subdomain) && !subdomain.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                var tenantBySubdomain = await platformDbContext.Tenants
                    .Where(x => x.Subdomain == subdomain && x.IsActive)
                    .Select(x => new TenantResolutionResult(x.Code, x.Id.ToString(), x.ConnectionString))
                    .FirstOrDefaultAsync(cancellationToken);
                return tenantBySubdomain;
            }
        }

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return null;
        }

        var normalized = tenantCode.Trim().ToUpperInvariant();
        return await platformDbContext.Tenants
            .Where(x => x.Code == normalized && x.IsActive)
            .Select(x => new TenantResolutionResult(x.Code, x.Id.ToString(), x.ConnectionString))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
