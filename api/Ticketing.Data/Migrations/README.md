# Migrations

The full initial schema lives in a single migration, `20260706120000_InitialSchema`,
written as raw PostgreSQL DDL. It is applied automatically on API startup via
`Database.Migrate()` (see `Program.cs`), so a clean database is brought up to the
current schema with `docker compose up --build` and contains only the schema plus
EF migration metadata — no seed data.

## No model snapshot committed

A design-time `ModelSnapshot` is intentionally not committed. It is only used by the
EF design-time tools (`dotnet ef migrations add/remove`) and has no effect at runtime.

If you later need to add a migration with the EF tools, first generate a snapshot that
matches the current model. The simplest way without a host .NET SDK is to run the tools
inside the build image, e.g.:

```bash
docker run --rm -v "$PWD/api:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -lc "dotnet tool install --global dotnet-ef && \
            export PATH=\$PATH:/root/.dotnet/tools && \
            dotnet ef migrations add <Name> --project Ticketing.Data --startup-project Ticketing.Api"
```

Review the generated migration before committing.
