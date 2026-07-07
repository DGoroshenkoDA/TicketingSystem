using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ticketing.Data;
using Ticketing.Data.Entities;
using Ticketing.Services.Common;
using Ticketing.Services.Email;

namespace Ticketing.Services.Auth;

public class AuthService : IAuthService
{
    private readonly TicketingDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _email;
    private readonly AppOptions _appOptions;

    public AuthService(
        TicketingDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailSender email,
        IOptions<AppOptions> appOptions)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _email = email;
        _appOptions = appOptions.Value;
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
            IsVerified = false,
            CreatedAt = now,
            ModifiedAt = now
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            // Lost a race with a concurrent signup for the same email; the DB
            // unique index rejected the insert. Surface the same conflict as the pre-check.
            return Error.Conflict("Auth.EmailTaken", "This email address is already registered.");
        }

        await IssueVerificationAsync(user, ct);

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

        if (_appOptions.RequireEmailVerification && !user.IsVerified)
        {
            return Error.Forbidden("Auth.EmailNotVerified", "Please verify your email address before signing in.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<ErrorOr<AuthResult>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashToken(request.RefreshToken);
        var now = DateTime.UtcNow;

        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= now || stored.User is null)
        {
            return Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token.");
        }

        // Defense in depth: an unverified user must not obtain fresh tokens on refresh,
        // mirroring the check in LoginAsync (verification may have been revoked since issuance).
        if (_appOptions.RequireEmailVerification && !stored.User.IsVerified)
        {
            return Error.Forbidden("Auth.EmailNotVerified", "Please verify your email address before signing in.");
        }

        stored.RevokedAt = now;
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task<ErrorOr<Success>> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _tokens.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.RevokedAt == null, ct);

        if (stored is not null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Error.Validation("Auth.InvalidVerificationToken", "The verification link is invalid.");
        }

        var hash = _tokens.HashToken(token);
        var now = DateTime.UtcNow;

        var stored = await _db.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || stored.UsedAt is not null || stored.ExpiresAt <= now || stored.User is null)
        {
            return Error.Validation("Auth.InvalidVerificationToken", "The verification link is invalid or has expired.");
        }

        stored.UsedAt = now;
        stored.User.IsVerified = true;
        stored.User.ModifiedAt = now;
        await _db.SaveChangesAsync(ct);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResendVerificationAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailNormalized == normalized, ct);

        // Do not reveal whether the account exists; always report success.
        if (user is not null && !user.IsVerified)
        {
            await IssueVerificationAsync(user, ct);
        }

        return Result.Success;
    }

    private async Task IssueVerificationAsync(User user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Invalidate any earlier unused tokens for this user.
        var active = await _db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in active)
        {
            t.UsedAt = now;
        }

        var (rawToken, tokenHash, expiresAt) = _tokens.CreateVerificationToken();
        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);

        var baseUrl = _appOptions.UiBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/verify?token={rawToken}";
        await _email.SendVerificationEmailAsync(user.Email, link, ct);
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
