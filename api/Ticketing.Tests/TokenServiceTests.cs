using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Ticketing.Data.Entities;
using Ticketing.Services.Auth;
using Xunit;

namespace Ticketing.Tests;

public class TokenServiceTests
{
    private readonly TokenService _tokens = TestSupport.NewTokenService();
    private readonly JwtOptions _options = TestSupport.JwtOptions();

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        DisplayName = "Sample User",
        PasswordHash = "x"
    };

    [Fact]
    public void AccessToken_Carries_Claims_And_Validates()
    {
        var user = SampleUser();

        var (token, expiresAt) = _tokens.CreateAccessToken(user);

        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        var principal = handler.ValidateToken(token, validationParameters, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Email, principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(user.DisplayName, principal.FindFirstValue("name"));
    }

    [Fact]
    public void RefreshToken_Hash_Is_Deterministic()
    {
        var (token, tokenHash, expiresAt) = _tokens.CreateRefreshToken();

        Assert.True(expiresAt > DateTime.UtcNow);
        Assert.Equal(tokenHash, _tokens.HashToken(token));
    }

    [Fact]
    public void Different_RefreshTokens_Hash_Differently()
    {
        var (_, hashA, _) = _tokens.CreateRefreshToken();
        var (_, hashB, _) = _tokens.CreateRefreshToken();

        Assert.NotEqual(hashA, hashB);
    }
}
