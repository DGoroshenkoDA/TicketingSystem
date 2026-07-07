namespace Ticketing.Services.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string From { get; set; } = "no-reply@dataart.com";
    public string? User { get; set; }
    public string? Password { get; set; }

    // Upper bound (seconds) for the SMTP connect/send round-trip. Keeps a slow or
    // unreachable relay from letting a background send linger for minutes.
    public int TimeoutSeconds { get; set; } = 10;
}
