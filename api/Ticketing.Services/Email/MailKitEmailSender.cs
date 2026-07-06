using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ticketing.Services.Email;

// Sends verification emails via SMTP (relay1.dataart.com by default).
// The verification link is ALWAYS written to the logs, so verification also
// works locally when the relay is unreachable or no mailbox is available.
public class MailKitEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<SmtpOptions> options, ILogger<MailKitEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default)
    {
        _logger.LogInformation("Email verification link for {Email}: {Link}", toEmail, verificationLink);

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP host is not configured; skipping actual email send.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_options.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Verify your Ticketing System account";
            message.Body = new TextPart("plain")
            {
                Text =
                    $"Welcome to the Ticketing System.\n\n" +
                    $"Please verify your email by opening this link (valid for 24 hours):\n{verificationLink}\n"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, ct);

            if (!string.IsNullOrWhiteSpace(_options.User))
            {
                await client.AuthenticateAsync(_options.User, _options.Password ?? string.Empty, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Do not fail sign-up if the relay is unreachable; the link is already logged.
            _logger.LogWarning(ex, "Failed to send verification email to {Email} via SMTP.", toEmail);
        }
    }
}
