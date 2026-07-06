namespace Ticketing.Services.Email;

public interface IEmailSender
{
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default);
}
