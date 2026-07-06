using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Data.Entities;

namespace Ticketing.Services.Auth;

public class AuthService : IAuthService
{
    private readonly TicketingDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(TicketingDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<ErrorOr<UserDto>> SignupAsync(SignupRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var normalized = email.ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.EmailNormalized == normalized, ct))
        {
            return Error.Conflict("Auth.EmailTaken", "This email address is already registered.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailNormalized = normalized,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAt = now,
            ModifiedAt = now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.Email, user.DisplayName);
    }

    public async Task<ErrorOr<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailNormalized == normalized, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<ErrorOr<AuthResult>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken);
        var now = DateTime.UtcNow;

        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= now || stored.User is null)
        {
            return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");
        }

        // Rotate: revoke the presented token and issue a fresh pair.
        stored.RevokedAt = now;
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task<ErrorOr<Success>> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.RevokedAt == null, ct);

        if (stored is not null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // Idempotent: logging out an unknown/already-revoked token still succeeds.
        return Result.Success;
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpiresAt) = _tokens.CreateAccessToken(user);
        var (refreshToken, refreshHash, refreshExpiresAt) = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return new AuthResult(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            new UserDto(user.Id, user.Email, user.DisplayName));
    }
}
