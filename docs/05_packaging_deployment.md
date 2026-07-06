# Packaging and Deployment

## Primary requirement

From a clean checkout, the entire product comes up with **a single command from the repository root**:

```
docker compose up --build
```

On a clean Windows, macOS, or Linux laptop, nothing beyond Docker (+ Docker Compose) should be required on the host. No .NET SDK, no Elixir, no Node, no Postgres on the host — everything inside containers.

## Docker Compose services

| Service | Image / build | Role |
|---------|---------------|------|
| `db` | `postgres:16` | RDBMS. Volume for data, `pg_isready` healthcheck. |
| `api` | multi-stage Dockerfile (`sdk:8` build → `aspnet:8` runtime) | REST API. Applies EF migrations on startup, waits for a healthy `db`. |
| `ui` | multi-stage Dockerfile (`mix deps.get` + `assets.deploy` + `mix release`) | Phoenix UI. Waits for a healthy `api`, knows its address over the compose network. |

Startup order — via `depends_on: { condition: service_healthy }`. Both `api` and `ui` have their own healthcheck endpoints.

## Building images (multi-stage)

- **api:** stage 1 — `dotnet restore`/`publish` on the SDK image; stage 2 — `aspnet:8` runtime image with the published artifacts. Migrations are applied on container startup.
- **ui:** stage 1 — install deps, `mix assets.deploy` (Tailwind + esbuild, minify + digest), `mix release`; stage 2 — a minimal runtime with the release.

## Configuration and secrets

- All configuration — via environment variables. The repository holds `.env.example` with placeholders; the real `.env` is not committed.
- No secrets in source.

Variables (minimum):

| Variable | Purpose |
|----------|---------|
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Database initialization |
| `ConnectionStrings__Default` | API connection string to `db` |
| `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryMinutes` | JWT settings |
| `Api__BaseUrl` | API address for the UI (compose internal network) |
| `SECRET_KEY_BASE` | Phoenix session/signing secret |

## Internal network

- Services communicate over the compose network by name (`db`, `api`, `ui`).
- Only the necessary ports are published externally (UI and, if needed, API/Swagger).
- The UI reaches the API by its internal address, not by a public host.

## Data and persistence

- Postgres data lives in a named volume so that restarting containers does not lose data.
- After migrations, a fresh database is empty (schema and migration metadata only); there is no seed data.

## README (required)

The root README must cover:
- Prerequisites (Docker + Docker Compose).
- Copying `.env.example` → `.env` and filling in values.
- The startup command `docker compose up --build`.
- UI/API addresses after startup and how to check health.
- How to run the tests.

## Pre-submission checklist

- Clean clone → `cp .env.example .env` → `docker compose up --build` → all three containers healthy.
- The UI opens in Chrome/Edge/Firefox, and the full flow (profile → login → teams/epics/tickets → board → drag & drop → comments) works.
- After a container restart, data is still there.
- No secrets or hard-coded passwords in source.
