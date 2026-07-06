using Ticketing.Services.Email;

namespace Ticketing.Tests;

// Captures the verification link instead of sending an email.
public class FakeEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }
    public string? LastLink { get; private set; }

    public string? LastToken =>
        LastLink is not null && LastLink.Contains("token=")
            ? LastLink.Split("token=")[^1]
            : null;

    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default)
    {
        LastEmail = toEmail;
        LastLink = verificationLink;
        return Task.CompletedTask;
    }
}
