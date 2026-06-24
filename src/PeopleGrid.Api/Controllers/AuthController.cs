using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Auth.DTOs;
using PeopleGrid.Application.Security;
using PeopleGrid.Infrastructure.Services;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Token refreshed"));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Logout successful"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(response, "If the account exists, password reset instructions have been sent"));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password reset successful"));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));
    }

    [Authorize]
    [HasPermission("User.Edit")]
    [HttpPost("unlock-account/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UnlockAccount(Guid userId, CancellationToken cancellationToken)
    {
        await authService.UnlockAccountAsync(userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Account unlocked successfully"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> Me(CancellationToken cancellationToken)
    {
        var response = await authService.GetCurrentUserAsync(cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(response));
    }
}
