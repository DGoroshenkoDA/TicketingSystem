# Ticketing System

A Kanban-style ticket tracker built as three separated tiers:

- **UI** — Phoenix 1.7 + LiveView (talks to the API over HTTP)
- **API** — .NET 8 ASP.NET Core Web API + EF Core (PostgreSQL)
- **DB** — PostgreSQL 16

Everything runs in Docker; the whole stack starts from the repository root with a single command.

## Prerequisites

- Docker and Docker Compose. Nothing else is required on the host (no .NET SDK, Elixir, Node, or Postgres).

## Configuration

```bash
cp .env.example .env
```

Then edit `.env` and set real values (database password, `JWT_SECRET`, `SECRET_KEY_BASE`).
Generate a Phoenix secret with `mix phx.gen.secret` if you have Elixir locally, or use any long random string.

`.env` is git-ignored and must not be committed.

## Start

From the repository root:

```bash
docker compose up --build
```

This builds and starts three containers: `db`, `api`, `ui`.

## Addresses

| Service | URL |
|---------|-----|
| UI | http://localhost:4000 |
| API | http://localhost:5080 |
| API health | http://localhost:5080/health |
| UI health | http://localhost:4000/health |
| Postgres | localhost:5432 |

Both health endpoints return `{"status":"healthy"}` once the containers are up.

## Tests

```bash
# Backend (from ./api)
dotnet test

# UI (from ./ui)
mix test
```

## Project layout

```
.
├─ docker-compose.yml   # db + api + ui
├─ .env.example         # configuration template
├─ api/                 # .NET 8 solution (Api / Services / Data / Common / Tests)
├─ ui/                  # Phoenix app (no Ecto; HTTP client to the API)
└─ docs/                # architecture and requirements
```

See `docs/ARCHITECTURE_AND_PLAN.md` for architecture, requirements, and the implementation plan.
