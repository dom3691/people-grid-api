using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Tenancy;

public sealed class CurrentTenantService : ICurrentTenantService
{
    public string? TenantCode { get; private set; }
    public string? TenantId { get; private set; }
    public string? ConnectionString { get; private set; }

    public void SetTenant(string tenantCode, string? tenantId, string? connectionString)
    {
        TenantCode = tenantCode;
        TenantId = tenantId;
        ConnectionString = connectionString;
    }
}
