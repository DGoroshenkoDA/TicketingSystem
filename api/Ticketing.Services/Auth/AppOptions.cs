namespace Ticketing.Services.Auth;

public class AppOptions
{
    public const string SectionName = "App";

    // Base URL of the UI, used to build email-verification links.
    public string UiBaseUrl { get; set; } = "http://localhost:4000";

    // When false (dev convenience), login does not block unverified users.
    public bool RequireEmailVerification { get; set; } = true;
}
