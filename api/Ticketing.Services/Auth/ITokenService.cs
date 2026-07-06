using Ticketing.Data.Entities;

namespace Ticketing.Services.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    // Returns the raw refresh token (returned to the client) and its hash (stored in the DB).
    (string Token, string TokenHash, DateTime ExpiresAt) CreateRefreshToken();

    // Returns a URL-safe email-verification token (valid 24h), its hash, and expiry.
    (string Token, string TokenHash, DateTime ExpiresAt) CreateVerificationToken();

    string HashToken(string token);
}
