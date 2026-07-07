using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data.Entities;
using Ticketing.Services.Auth;
using Xunit;

namespace Ticketing.Tests;

public class AuthServiceScenarioTests
{
    private static SignupRequest Signup(string email = "user@example.com", string password = "s3cretPassword")
        => new(email, "Sample User", password, password);

    private static string TokenFrom(FakeEmailSender email) => email.LastToken!;

    private static async Task SignupAndVerify(AuthService auth, FakeEmailSender email, string address, string password = "s3cretPassword")
    {
        await auth.SignupAsync(Signup(address, password));
        await auth.VerifyEmailAsync(TokenFrom(email));
    }

    [Fact]
    public async Task Signup_Creates_Unverified_User_And_Sends_Link()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);

        var result = await auth.SignupAsync(Signup("  User@Example.com  "));

        Assert.False(result.IsError);
        var user = await db.Users.SingleAsync();
        Assert.Equal("user@example.com", user.EmailNormalized);
        Assert.False(user.IsVerified);
        Assert.NotNull(email.LastLink);
        Assert.NotNull(email.LastToken);
    }

    [Fact]
    public async Task Signup_Duplicate_Email_Returns_Conflict()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);

        await auth.SignupAsync(Signup("dup@example.com"));
        var second = await auth.SignupAsync(Signup("DUP@example.com"));

        Assert.True(second.IsError);
        Assert.Equal(ErrorType.Conflict, second.FirstError.Type);
    }

    [Fact]
    public async Task Verify_Marks_User_Verified()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await auth.SignupAsync(Signup("verify@example.com"));

        var result = await auth.VerifyEmailAsync(TokenFrom(email));

        Assert.False(result.IsError);
        Assert.True((await db.Users.SingleAsync()).IsVerified);
    }

    [Fact]
    public async Task Verify_Invalid_Token_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);

        var result = await auth.VerifyEmailAsync("not-a-real-token");

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Login_Before_Verification_Returns_Forbidden()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("pending@example.com"));

        var result = await auth.LoginAsync(new LoginRequest("pending@example.com", "s3cretPassword"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
    }

    [Fact]
    public async Task Login_After_Verification_Returns_Tokens()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await SignupAndVerify(auth, email, "login@example.com");

        var result = await auth.LoginAsync(new LoginRequest("login@example.com", "s3cretPassword"));

        Assert.False(result.IsError);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task Login_Wrong_Password_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await SignupAndVerify(auth, email, "who@example.com");

        var result = await auth.LoginAsync(new LoginRequest("who@example.com", "wrongPassword"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Login_Unknown_Email_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest("nobody@example.com", "whatever123"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Refresh_With_Valid_Token_Rotates_And_Revokes_Old()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await SignupAndVerify(auth, email, "rot@example.com");
        var login = (await auth.LoginAsync(new LoginRequest("rot@example.com", "s3cretPassword"))).Value;

        var refreshed = await auth.RefreshAsync(new RefreshRequest(login.RefreshToken));

        Assert.False(refreshed.IsError);
        Assert.NotEqual(login.RefreshToken, refreshed.Value.RefreshToken);
        Assert.Equal(2, await db.RefreshTokens.CountAsync());
        Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.RevokedAt != null));
    }

    [Fact]
    public async Task Refresh_With_Invalid_Token_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);

        var result = await auth.RefreshAsync(new RefreshRequest("not-a-real-token"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Refresh_For_Unverified_User_Returns_Forbidden()
    {
        using var db = TestSupport.NewDb();
        var tokens = TestSupport.NewTokenService();
        // AddUser creates an unverified user (IsVerified defaults to false).
        var userId = TestSupport.AddUser(db, "unverified@example.com");
        var auth = TestSupport.NewAuthService(db);

        const string raw = "a-valid-raw-refresh-token";
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokens.HashToken(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await auth.RefreshAsync(new RefreshRequest(raw));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Forbidden, result.FirstError.Type);
        // No new tokens issued and the presented token is not rotated/revoked.
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
        Assert.Equal(0, await db.RefreshTokens.CountAsync(r => r.RevokedAt != null));
    }

    [Fact]
    public async Task Logout_Revokes_Token_And_Is_Idempotent()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await SignupAndVerify(auth, email, "out@example.com");
        var login = (await auth.LoginAsync(new LoginRequest("out@example.com", "s3cretPassword"))).Value;

        var first = await auth.LogoutAsync(new LogoutRequest(login.RefreshToken));
        var second = await auth.LogoutAsync(new LogoutRequest(login.RefreshToken));

        Assert.False(first.IsError);
        Assert.False(second.IsError);
        Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.RevokedAt != null));
    }

    [Fact]
    public async Task Resend_Invalidates_Old_Token_And_Creates_New()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await auth.SignupAsync(Signup("resend@example.com"));

        var result = await auth.ResendVerificationAsync("resend@example.com");

        Assert.False(result.IsError);
        Assert.Equal(2, await db.EmailVerificationTokens.CountAsync());
        Assert.Equal(1, await db.EmailVerificationTokens.CountAsync(t => t.UsedAt != null));
    }

    [Fact]
    public async Task Verify_Token_Is_Single_Use()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await auth.SignupAsync(Signup("single@example.com"));
        var token = TokenFrom(email);

        var first = await auth.VerifyEmailAsync(token);
        var second = await auth.VerifyEmailAsync(token);

        Assert.False(first.IsError);
        Assert.True(second.IsError);
        Assert.Equal(ErrorType.Validation, second.FirstError.Type);
    }

    [Fact]
    public async Task Verify_Expired_Token_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        var tokens = TestSupport.NewTokenService();
        var userId = TestSupport.AddUser(db, "expired@example.com");

        const string raw = "an-expired-raw-token";
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokens.HashToken(raw),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        });
        await db.SaveChangesAsync();

        var result = await auth.VerifyEmailAsync(raw);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Resend_For_Already_Verified_User_Is_NoOp_But_Succeeds()
    {
        using var db = TestSupport.NewDb();
        var email = new FakeEmailSender();
        var auth = TestSupport.NewAuthService(db, email);
        await SignupAndVerify(auth, email, "done@example.com");
        var tokenCountBefore = await db.EmailVerificationTokens.CountAsync();

        var result = await auth.ResendVerificationAsync("done@example.com");

        Assert.False(result.IsError);
        // No new token is issued for an already-verified user.
        Assert.Equal(tokenCountBefore, await db.EmailVerificationTokens.CountAsync());
    }
}
