namespace PeopleGrid.Application.Auth.DTOs;

public sealed record LoginRequest(string TenantCode, string EmailOrUserName, string Password);
public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string TenantCode, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record CurrentUserResponse(Guid UserId, string Email, string UserName, string FullName, string TenantCode, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record RefreshTokenRequest(string TenantCode, string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string TenantCode, string EmailOrUserName);
public sealed record ForgotPasswordResponse(DateTime? ExpiresAt);
public sealed record ResetPasswordRequest(string TenantCode, string ResetToken, string NewPassword, string ConfirmPassword);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
