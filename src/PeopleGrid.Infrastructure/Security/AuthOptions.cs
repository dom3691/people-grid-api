namespace PeopleGrid.Infrastructure.Security;

public sealed class AuthOptions
{
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public int PasswordResetTokenMinutes { get; set; } = 30;
    public int PasswordHistoryLimit { get; set; } = 5;
    public int PasswordExpiryDays { get; set; } = 90;
}
