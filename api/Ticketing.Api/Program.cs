using Microsoft.EntityFrameworkCore;
using Ticketing.Data;

var builder = WebApplication.CreateBuilder(args);

// EF Core / PostgreSQL. snake_case mapping matches the migration DDL.
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Phase 0/1 skeleton. Auth, controllers, validation are wired up in later phases.

var app = builder.Build();

// Apply migrations on startup (with a short retry in case the DB is still coming up).
// Skipped under the "Testing" environment, where there is no database.
if (!app.Environment.IsEnvironment("Testing"))
{
    await ApplyMigrationsAsync(app);
}

// Public readiness/liveness endpoint (allowed to be public per requirements).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Migration attempt {Attempt}/{Max} failed; retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    // Final attempt: let it throw so the container fails loudly.
    await db.Database.MigrateAsync();
}

// Exposed so the WebApplicationFactory-based tests can reference the entry point.
public partial class Program { }
