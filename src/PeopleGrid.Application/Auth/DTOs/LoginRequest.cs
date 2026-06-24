namespace PeopleGrid.Application.Auth.DTOs;

public sealed record LoginRequest(string TenantCode, string EmailOrUserName, string Password);
public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string TenantCode, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
