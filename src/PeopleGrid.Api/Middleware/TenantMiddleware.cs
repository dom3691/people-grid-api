using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Api.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, ICurrentTenantService currentTenant)
    {
        var tenant = await resolver.ResolveAsync(context.RequestAborted);
        if (tenant is not null)
        {
            currentTenant.SetTenant(tenant.TenantCode, tenant.TenantId, tenant.ConnectionString);
        }

        await next(context);
    }
}
