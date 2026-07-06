namespace Ticketing.Services.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string From { get; set; } = "no-reply@ticketing.local";
    public string? User { get; set; }
    public string? Password { get; set; }
}
