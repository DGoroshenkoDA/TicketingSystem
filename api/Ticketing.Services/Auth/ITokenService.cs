using Ticketing.Data.Entities;

namespace Ticketing.Services.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    // Returns the raw refresh token (returned to the client) and its hash (stored in the DB).
    (string Token, string TokenHash, DateTime ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string token);
}
