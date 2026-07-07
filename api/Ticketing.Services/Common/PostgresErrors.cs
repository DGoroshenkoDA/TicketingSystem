using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ticketing.Services.Common;

// Helpers for interpreting database-level integrity errors.
internal static class PostgresErrors
{
    // PostgreSQL SQLSTATE for unique_violation.
    private const string UniqueViolation = "23505";

    // True when a SaveChanges failed because a unique index/constraint was
    // violated. This is the DB-level guard that fires when a TOCTOU race beats
    // an in-service uniqueness pre-check.
    public static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: UniqueViolation };
}
