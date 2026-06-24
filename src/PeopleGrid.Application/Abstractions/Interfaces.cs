using System.Security.Claims;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrentTenantService
{
    string? TenantCode { get; }
    string? TenantId { get; }
    string? ConnectionString { get; }
    void SetTenant(string tenantCode, string? tenantId, string? connectionString);
}

public interface ITenantResolver
{
    Task<TenantResolutionResult?> ResolveAsync(CancellationToken cancellationToken = default);
}

public sealed record TenantResolutionResult(string TenantCode, string? TenantId, string? ConnectionString);

public interface ITenantConnectionProvider
{
    string GetTenantConnectionString();
    Task<string?> GetConnectionStringByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);
}

public interface ITenantProvisioningService
{
    Task ProvisionTenantDatabaseAsync(Tenant tenant, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions, string tenantCode);
    string GenerateRefreshToken();
}

public interface IAuditService
{
    Task TrackAsync(string module, string action, string entityType, string? entityId = null, string outcome = "Success", CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
