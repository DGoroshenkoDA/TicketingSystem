var builder = WebApplication.CreateBuilder(args);

// Phase 0: minimal skeleton. DI, EF Core, JWT, controllers, and validation
// are wired up in later phases.

var app = builder.Build();

// Public readiness/liveness endpoint (allowed to be public per requirements).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Exposed so the WebApplicationFactory-based tests can reference the entry point.
public partial class Program { }
