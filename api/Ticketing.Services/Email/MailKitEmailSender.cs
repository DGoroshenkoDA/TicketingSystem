using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ticketing.Services.Email;

// Sends verification emails via SMTP (relay1.dataart.com by default).
// The network send runs in the background with a bounded timeout, so sign-up and
// resend never block on a slow or unreachable relay. The raw verification link
// carries a single-use token, so it is only logged at Debug on the happy path;
// when the relay is unreachable or unconfigured the link is surfaced at Warning
// instead, so verification still works locally without a reachable relay.
public class MailKitEmailSender : IEmailSender
{
    // Used when Smtp:From is unset or empty. The config binder overwrites the
    // SmtpOptions.From default with an empty string when the env var is present
    // but blank (e.g. Smtp__From= in a raw prod deploy), so we fall back here
    // rather than let MailboxAddress.Parse("") throw and drop the email.
    private const string DefaultFrom = "no-reply@dataart.com";

    private readonly SmtpOptions _options;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<SmtpOptions> options, ILogger<MailKitEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default)
    {
        // The link contains the raw single-use token; keep it out of Information-level logs.
        _logger.LogDebug("Email verification link for {Email}: {Link}", toEmail, verificationLink);

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            // No relay configured (dev): surface the link so verification still works locally.
            _logger.LogWarning(
                "SMTP host is not configured; skipping email send. Verification link for {Email}: {Link}",
                toEmail, verificationLink);
            return Task.CompletedTask;
        }

        // Deliver in the background: the caller (sign-up / resend) must not wait on the
        // SMTP round-trip. A fresh timeout token is used instead of the request token,
        // which is cancelled once the HTTP response completes.
        _ = Task.Run(() => DeliverAsync(toEmail, verificationLink));
        return Task.CompletedTask;
    }

    private async Task DeliverAsync(string toEmail, string verificationLink)
    {
        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 10);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var fromAddress = string.IsNullOrWhiteSpace(_options.From) ? DefaultFrom : _options.From;

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Verify your Ticketing System account";
            message.Body = new TextPart("plain")
            {
                Text =
                    $"Welcome to the Ticketing System.\n\n" +
                    $"Please verify your email by opening this link (valid for 24 hours):\n{verificationLink}\n"
            };

            using var client = new SmtpClient { Timeout = (int)timeout.TotalMilliseconds };
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.Auto, cts.Token);

            if (!string.IsNullOrWhiteSpace(_options.User))
            {
                await client.AuthenticateAsync(_options.User, _options.Password ?? string.Empty, cts.Token);
            }

            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);

            // Success path: do not log the raw token.
            _logger.LogInformation("Verification email sent to {Email}.", toEmail);
        }
        catch (Exception ex)
        {
            // Relay unreachable/timed out: surface the link at Warning (with the raw token)
            // so a developer can still complete verification without the relay.
            _logger.LogWarning(
                ex, "Failed to send verification email to {Email} via SMTP. Verification link: {Link}",
                toEmail, verificationLink);
        }
    }
}
