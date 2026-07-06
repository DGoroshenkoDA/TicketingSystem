using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ticketing.Data;
using Ticketing.Data.Entities;
using Ticketing.Services.Auth;
using Ticketing.Services.Comments;
using Ticketing.Services.Epics;
using Ticketing.Services.Teams;
using Ticketing.Services.Tickets;

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

    public static TeamService NewTeamService(TicketingDbContext db) => new(db);

    public static EpicService NewEpicService(TicketingDbContext db) => new(db);

    public static TicketService NewTicketService(TicketingDbContext db) => new(db);

    public static CommentService NewCommentService(TicketingDbContext db) => new(db);

    public static Guid AddUser(TicketingDbContext db, string email = "user@example.com")
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailNormalized = email.Trim().ToLowerInvariant(),
            DisplayName = "Test User",
            PasswordHash = "x",
            CreatedAt = now,
            ModifiedAt = now
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    // Adds a ticket directly (no TicketService yet) for integrity-rule tests.
    public static Ticket AddTicket(TicketingDbContext db, Guid teamId, Guid? epicId = null)
    {
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Type = "bug",
            State = "new",
            EpicId = epicId,
            Title = "Sample",
            Body = "Body",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = now,
            ModifiedAt = now
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }
}
