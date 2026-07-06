using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Services.Auth;
using Xunit;

namespace Ticketing.Tests;

public class AuthServiceScenarioTests
{
    private static SignupRequest Signup(string email = "user@example.com", string password = "s3cretPassword")
        => new(email, "Sample User", password, password);

    [Fact]
    public async Task Signup_Creates_User_With_Normalized_Email()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);

        var result = await auth.SignupAsync(Signup("  User@Example.com  "));

        Assert.False(result.IsError);
        var user = await db.Users.SingleAsync();
        Assert.Equal("User@Example.com", user.Email);
        Assert.Equal("user@example.com", user.EmailNormalized);
        Assert.NotEqual("s3cretPassword", user.PasswordHash);
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
    public async Task Login_After_Signup_Returns_Tokens()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("login@example.com"));

        var result = await auth.LoginAsync(new LoginRequest("login@example.com", "s3cretPassword"));

        Assert.False(result.IsError);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Equal("login@example.com", result.Value.User.Email);
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task Login_Wrong_Password_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("who@example.com"));

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
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("rot@example.com"));
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
    public async Task Refresh_With_Revoked_Token_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("rev@example.com"));
        var login = (await auth.LoginAsync(new LoginRequest("rev@example.com", "s3cretPassword"))).Value;

        await auth.LogoutAsync(new LogoutRequest(login.RefreshToken));
        var result = await auth.RefreshAsync(new RefreshRequest(login.RefreshToken));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task Logout_Revokes_Token_And_Is_Idempotent()
    {
        using var db = TestSupport.NewDb();
        var auth = TestSupport.NewAuthService(db);
        await auth.SignupAsync(Signup("out@example.com"));
        var login = (await auth.LoginAsync(new LoginRequest("out@example.com", "s3cretPassword"))).Value;

        var first = await auth.LogoutAsync(new LogoutRequest(login.RefreshToken));
        var second = await auth.LogoutAsync(new LogoutRequest(login.RefreshToken));

        Assert.False(first.IsError);
        Assert.False(second.IsError);
        Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.RevokedAt != null));
    }
}
