using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ticketing.Data;
using Ticketing.Services.Auth;

namespace Ticketing.Tests;

internal static class TestSupport
{
    public static TicketingDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TicketingDbContext(options);
    }

    public static JwtOptions JwtOptions() => new()
    {
        Secret = "test_secret_key_that_is_long_enough_1234567890",
        Issuer = "ticketing-api",
        Audience = "ticketing-ui",
        ExpiryMinutes = 120,
        RefreshTokenDays = 14
    };

    public static TokenService NewTokenService() => new(Options.Create(JwtOptions()));

    public static IPasswordHasher NewHasher() => new Argon2idPasswordHasher();

    public static AuthService NewAuthService(TicketingDbContext db)
        => new(db, NewHasher(), NewTokenService());
}
