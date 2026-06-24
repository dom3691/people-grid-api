namespace PeopleGrid.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "PeopleGrid";
    public string Audience { get; set; } = "PeopleGrid";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_LONG_SECURE_KEY_FOR_PRODUCTION";
    public int AccessTokenMinutes { get; set; } = 60;
}
