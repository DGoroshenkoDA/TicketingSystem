using Microsoft.EntityFrameworkCore;
using Ticketing.Data;

var builder = WebApplication.CreateBuilder(args);

// EF Core / PostgreSQL. snake_case mapping matches the migration DDL.
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Phase 0/1 skeleton. Auth, controllers, validation are wired up in lat