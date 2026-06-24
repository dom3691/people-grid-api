using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Auth.DTOs;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Security;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    ITenantConnectionProvider tenantConnectionProvider,
    ICurrentTenantService currentTenant,
    ICurrentUserService currentUser,
    IApplicationDbContextFactory dbContextFactory,
    IJwtTokenService jwtTokenService,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthOptions> authOptions,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedTenantCode = NormalizeTenantCode(request.TenantCode);
        var connectionString = await tenantConnectionProvider.GetConnectionStringByTenantCodeAsync(normalizedTenantCode, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid credentials.");

        currentTenant.SetTenant(normalizedTenantCode, null, connectionString);
        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);

        var emailOrUserName = NormalizeEmailOrUserName(request.EmailOrUserName);
        var user = await LoadUserWithAccessAsync(dbContext, emailOrUserName, cancellationToken);
        if (user is null)
        {
            await RecordLoginAttemptAsync(dbContext, null, emailOrUserName, false, "InvalidCredentials", cancellationToken);
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        if (!user.IsActive || user.IsDeleted)
        {
            await RecordLoginAttemptAsync(dbContext, user, emailOrUserName, false, "InactiveAccount", cancellationToken);
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        if (user.LockoutEnd is not null && user.LockoutEnd > DateTime.UtcNow)
        {
            await RecordLoginAttemptAsync(dbContext, user, emailOrUserName, false, "AccountLocked", cancellationToken);
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await HandleFailedLoginAsync(dbContext, user, emailOrUserName, cancellationToken);
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        if (await IsPasswordExpiredAsync(dbContext, user, cancellationToken))
        {
            await RecordLoginAttemptAsync(dbContext, user, emailOrUserName, false, "PasswordExpired", cancellationToken);
            await AddAuditAsync(dbContext, user.Id.ToString(), "Auth", "PasswordExpired", "User", user.Id.ToString(), "Failed", cancellationToken);
            throw new UnauthorizedAppException("Password has expired.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await RecordLoginAttemptAsync(dbContext, user, emailOrUserName, true, null, cancellationToken);
        await AddAuditAsync(dbContext, user.Id.ToString(), "Auth", "Login", "User", user.Id.ToString(), "Success", cancellationToken);

        return await IssueTokensAsync(dbContext, user, normalizedTenantCode, cancellationToken);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedTenantCode = NormalizeTenantCode(request.TenantCode);
        var connectionString = await tenantConnectionProvider.GetConnectionStringByTenantCodeAsync(normalizedTenantCode, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid refresh token.");

        currentTenant.SetTenant(normalizedTenantCode, null, connectionString);
        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);

        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var existingToken = await dbContext.RefreshTokens
            .Include(x => x.User!)
            .ThenInclude(x => x.UserRoles)
            .ThenInclude(x => x.Role!)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid refresh token.");

        var hasActiveSession = await dbContext.UserSessions.AnyAsync(x => x.UserId == existingToken.UserId && x.RefreshTokenHash == tokenHash && x.IsActive, cancellationToken);
        if (!hasActiveSession || existingToken.RevokedAt is not null || existingToken.ExpiresAt <= DateTime.UtcNow || existingToken.User is null || !existingToken.User.IsActive)
        {
            await AddAuditAsync(dbContext, existingToken.UserId.ToString(), "Auth", "RefreshTokenRejected", "RefreshToken", existingToken.Id.ToString(), "Failed", cancellationToken);
            throw new UnauthorizedAppException("Invalid refresh token.");
        }

        existingToken.RevokedAt = DateTime.UtcNow;
        var oldSessions = await dbContext.UserSessions.Where(x => x.UserId == existingToken.UserId && x.RefreshTokenHash == tokenHash && x.IsActive).ToListAsync(cancellationToken);
        foreach (var session in oldSessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
        }
        var response = await IssueTokensAsync(dbContext, existingToken.User, normalizedTenantCode, cancellationToken);
        var replacementHash = TokenHasher.Hash(response.RefreshToken);
        var replacement = await dbContext.RefreshTokens.FirstAsync(x => x.TokenHash == replacementHash, cancellationToken);
        existingToken.ReplacedByTokenId = replacement.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(dbContext, existingToken.UserId.ToString(), "Auth", "RefreshTokenRotated", "RefreshToken", existingToken.Id.ToString(), "Success", cancellationToken);

        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var connectionString = currentTenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new UnauthorizedAppException("Tenant context was not resolved.");
        }

        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid refresh token.");

        token.RevokedAt = DateTime.UtcNow;
        var sessions = await dbContext.UserSessions.Where(x => x.RefreshTokenHash == tokenHash && x.IsActive).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(dbContext, token.UserId.ToString(), "Auth", "Logout", "User", token.UserId.ToString(), "Success", cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedTenantCode = NormalizeTenantCode(request.TenantCode);
        var connectionString = await tenantConnectionProvider.GetConnectionStringByTenantCodeAsync(normalizedTenantCode, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid tenant code.");

        currentTenant.SetTenant(normalizedTenantCode, null, connectionString);
        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);

        var emailOrUserName = NormalizeEmailOrUserName(request.EmailOrUserName);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == emailOrUserName || x.UserName == emailOrUserName, cancellationToken);
        var resetToken = jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(await GetIntSettingAsync(dbContext, "Auth.PasswordResetTokenMinutes", authOptions.Value.PasswordResetTokenMinutes, cancellationToken));

        if (user is not null && user.IsActive)
        {
            dbContext.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = TokenHasher.Hash(resetToken),
                ExpiresAt = expiresAt,
                RequestedIpAddress = GetIpAddress()
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await emailService.SendAsync(user.Email, "PeopleGrid password reset", $"Use this reset token to complete your password reset: {resetToken}", cancellationToken);
            await AddAuditAsync(dbContext, user.Id.ToString(), "Auth", "ForgotPassword", "User", user.Id.ToString(), "Success", cancellationToken);
        }

        return new ForgotPasswordResponse(user is null ? null : expiresAt);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedTenantCode = NormalizeTenantCode(request.TenantCode);
        var connectionString = await tenantConnectionProvider.GetConnectionStringByTenantCodeAsync(normalizedTenantCode, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid reset token.");

        currentTenant.SetTenant(normalizedTenantCode, null, connectionString);
        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);

        var tokenHash = TokenHasher.Hash(request.ResetToken);
        var token = await dbContext.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid reset token.");

        if (token.UsedAt is not null || token.ExpiresAt <= DateTime.UtcNow || token.User is null || !token.User.IsActive)
        {
            throw new UnauthorizedAppException("Invalid reset token.");
        }

        await EnsurePasswordNotReusedAsync(dbContext, token.User, request.NewPassword, cancellationToken);
        token.User.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        token.User.PasswordChangedAt = DateTime.UtcNow;
        token.User.FailedLoginCount = 0;
        token.User.LockoutEnd = null;
        token.UsedAt = DateTime.UtcNow;
        AddPasswordHistory(dbContext, token.User);

        await RevokeUserRefreshTokensAsync(dbContext, token.UserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(dbContext, token.UserId.ToString(), "Auth", "ResetPassword", "User", token.UserId.ToString(), "Success", cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
        {
            throw new UnauthorizedAppException("User context was not resolved.");
        }

        var connectionString = currentTenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new UnauthorizedAppException("Tenant context was not resolved.");
        }

        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("User context was not resolved.");

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid current password.");
        }

        await EnsurePasswordNotReusedAsync(dbContext, user, request.NewPassword, cancellationToken);
        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        AddPasswordHistory(dbContext, user);
        await RevokeUserRefreshTokensAsync(dbContext, user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(dbContext, user.Id.ToString(), "Auth", "ChangePassword", "User", user.Id.ToString(), "Success", cancellationToken);
    }

    public async Task UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connectionString = currentTenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new UnauthorizedAppException("Tenant context was not resolved.");
        }

        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(dbContext, currentUser.UserId, "Auth", "UnlockAccount", "User", user.Id.ToString(), "Success", cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId))
        {
            throw new UnauthorizedAppException("User context was not resolved.");
        }

        var connectionString = currentTenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(currentTenant.TenantCode))
        {
            throw new UnauthorizedAppException("Tenant context was not resolved.");
        }

        await using var dbContext = dbContextFactory.CreateDbContext(connectionString);
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role!)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("User context was not resolved.");

        var roles = user.UserRoles.Select(x => x.Role!.Name).Distinct().ToArray();
        var permissions = user.UserRoles.SelectMany(x => x.Role!.RolePermissions).Select(x => x.Permission!.Code).Distinct().ToArray();

        return new CurrentUserResponse(user.Id, user.Email, user.UserName, $"{user.FirstName} {user.LastName}".Trim(), currentTenant.TenantCode, roles, permissions);
    }

    private async Task<LoginResponse> IssueTokensAsync(IApplicationDbContext dbContext, User user, string tenantCode, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles.Select(x => x.Role!.Name).Distinct().ToArray();
        var permissions = user.UserRoles
            .SelectMany(x => x.Role!.RolePermissions)
            .Select(x => x.Permission!.Code)
            .Distinct()
            .ToArray();

        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = TokenHasher.Hash(refreshToken);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(await GetIntSettingAsync(dbContext, "Auth.RefreshTokenDays", authOptions.Value.RefreshTokenDays, cancellationToken));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });
        dbContext.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = refreshTokenHash,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            jwtTokenService.GenerateAccessToken(user, roles, permissions, tenantCode),
            refreshToken,
            DateTime.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes),
            tenantCode,
            roles,
            permissions);
    }

    private static async Task<User?> LoadUserWithAccessAsync(IApplicationDbContext dbContext, string emailOrUserName, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role!)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Email == emailOrUserName || x.UserName == emailOrUserName, cancellationToken);
    }

    private async Task HandleFailedLoginAsync(IApplicationDbContext dbContext, User user, string emailOrUserName, CancellationToken cancellationToken)
    {
        user.FailedLoginCount += 1;
        var maxAttempts = await GetIntSettingAsync(dbContext, "Auth.MaxFailedLoginAttempts", authOptions.Value.MaxFailedLoginAttempts, cancellationToken);
        if (user.FailedLoginCount >= maxAttempts)
        {
            var lockoutMinutes = await GetIntSettingAsync(dbContext, "Auth.LockoutMinutes", authOptions.Value.LockoutMinutes, cancellationToken);
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            await AddAuditAsync(dbContext, user.Id.ToString(), "Auth", "AccountLocked", "User", user.Id.ToString(), "Success", cancellationToken);
        }

        await RecordLoginAttemptAsync(dbContext, user, emailOrUserName, false, "InvalidCredentials", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordLoginAttemptAsync(IApplicationDbContext dbContext, User? user, string emailOrUserName, bool success, string? failureReason, CancellationToken cancellationToken)
    {
        dbContext.LoginAttempts.Add(new LoginAttempt
        {
            UserId = user?.Id,
            EmailOrUserName = emailOrUserName,
            Success = success,
            FailureReason = failureReason,
            IpAddress = GetIpAddress(),
            UserAgent = GetUserAgent()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> GetIntSettingAsync(IApplicationDbContext dbContext, string key, int fallback, CancellationToken cancellationToken)
    {
        var value = await dbContext.SystemSettings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private async Task<bool> IsPasswordExpiredAsync(IApplicationDbContext dbContext, User user, CancellationToken cancellationToken)
    {
        var expiryDays = await GetIntSettingAsync(dbContext, "Auth.PasswordExpiryDays", authOptions.Value.PasswordExpiryDays, cancellationToken);
        return expiryDays > 0 && user.PasswordChangedAt.AddDays(expiryDays) <= DateTime.UtcNow;
    }

    private static async Task RevokeUserRefreshTokensAsync(IApplicationDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    private async Task EnsurePasswordNotReusedAsync(IApplicationDbContext dbContext, User user, string newPassword, CancellationToken cancellationToken)
    {
        if (PasswordHasher.Verify(newPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("New password cannot be the same as a recently used password.");
        }

        var historyLimit = await GetIntSettingAsync(dbContext, "Auth.PasswordHistoryLimit", authOptions.Value.PasswordHistoryLimit, cancellationToken);
        var passwordHistories = await dbContext.PasswordHistories
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.ChangedAt)
            .Take(historyLimit)
            .ToListAsync(cancellationToken);

        if (passwordHistories.Any(x => PasswordHasher.Verify(newPassword, x.PasswordHash)))
        {
            throw new BusinessRuleException("New password cannot be the same as a recently used password.");
        }
    }

    private static void AddPasswordHistory(IApplicationDbContext dbContext, User user)
    {
        dbContext.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            ChangedAt = DateTime.UtcNow
        });
    }

    private async Task AddAuditAsync(IApplicationDbContext dbContext, string? actorUserId, string module, string action, string entityType, string? entityId, string outcome, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Module = module,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string? GetIpAddress() => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private static string NormalizeTenantCode(string tenantCode) => tenantCode.Trim().ToUpperInvariant();

    private static string NormalizeEmailOrUserName(string emailOrUserName) => emailOrUserName.Trim().ToLowerInvariant();
}
