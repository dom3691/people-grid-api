using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Auth.DTOs;
using PeopleGrid.Infrastructure.Persistence;
using PeopleGrid.Infrastructure.Security;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    ITenantConnectionProvider tenantConnectionProvider,
    ICurrentTenantService currentTenant,
    ApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IAuditService auditService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var connectionString = await tenantConnectionProvider.GetConnectionStringByTenantCodeAsync(request.TenantCode, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid tenant code.");
        currentTenant.SetTenant(request.TenantCode.Trim().ToUpperInvariant(), null, connectionString);

        var user = await dbContext.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)!.ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.EmailOrUserName || x.UserName == request.EmailOrUserName, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid credentials.");

        if (!user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await auditService.TrackAsync("Auth", "LoginFailed", "User", user.Id.ToString(), "Failed", cancellationToken);
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        var roles = user.UserRoles.Select(x => x.Role!.Name).Distinct().ToArray();
        var permissions = user.UserRoles
            .SelectMany(x => x.Role!.RolePermissions)
            .Select(x => x.Permission!.Code)
            .Distinct()
            .ToArray();

        user.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.TrackAsync("Auth", "Login", "User", user.Id.ToString(), "Success", cancellationToken);

        return new LoginResponse(
            jwtTokenService.GenerateAccessToken(user, roles, permissions, request.TenantCode.Trim().ToUpperInvariant()),
            jwtTokenService.GenerateRefreshToken(),
            DateTime.UtcNow.AddHours(1),
            request.TenantCode.Trim().ToUpperInvariant(),
            roles,
            permissions);
    }
}
