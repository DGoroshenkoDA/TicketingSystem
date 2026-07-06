namespace Ticketing.Services.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Access token lifetime in minutes (env: Jwt__ExpiryMinutes).
    public int ExpiryMinutes { get; set; } = 120;

    // Refresh token lifetime in days (env: Jwt__RefreshTokenDays).
    public int RefreshTokenDays { get; set; } = 14;
}
