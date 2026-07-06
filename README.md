# Ticketing System

A Kanban-style ticket tracker built as three separated tiers:

- **UI** — Phoenix 1.7 + LiveView (talks to the API over HTTP; no database of its own)
- **API** — .NET 8 ASP.NET Core Web API + EF Core (source of truth)
- **DB** — PostgreSQL 16

Everything runs in Docker; the whole stack starts from the repository root with a single command.

## Features

- **Accounts:** sign up (email, display name, password) and log in. Passwords are hashed with Argon2id; auth uses JWT access + refresh tokens (token stored in a server-side session cookie, never in the URL).
- **Teams:** list, create, rename, delete. Names are unique (case-insensitive). A team cannot be deleted while it has tickets or epics (HTTP 409); the delete button is disabled in the UI.
- **Epics:** per-team CRUD. The team is fixed after creation. An epic cannot be deleted while tickets reference it (409).
- **Tickets:** create/edit/details in a modal over the board. Fields: type (bug/feature/fix), state (5-column workflow), optional epic, title, body, plus server-set created/modified timestamps and creator. The epic must belong to the ticket's team (enforced in the DB and the API). `modified_at` only advances on a real field/state change.
- **Kanban board:** five columns in workflow order, team selector, cards showing type + epic + title, sorted most-recently-modified first. Filter by type and epic, plus case-insensitive title search (server-side). Drag & drop between columns persists the new state immediately and rolls back on failure.
- **Comments:** add comments to a ticket, shown oldest-first with author and timestamp. Comments are immutable and do not change the ticket's board ordering.

## Tech stack

| Tier | Stack |
|------|-------|
| UI | Phoenix 1.7, LiveView 1.0, Tailwind, esbuild, Req |
| API | .NET 8, ASP.NET Core, EF Core (Npgsql), FluentValidation, ErrorOr, Argon2id, JWT |
| DB | PostgreSQL 16 (schema via EF Core migrations) |
| Orchestration | Docker Compose |

## Prerequisites

- Docker and Docker Compose. Nothing else is required on the host (no .NET SDK, Elixir, Node, or Postgres).

## Configuration

```bash
cp .env.example .env
```

Then edit `.env` and set real values:

- `POSTGRES_PASSWORD` — any password.
- `JWT_SECRET` — a long random string (32+ chars).
- `SECRET_KEY_BASE` — a 64+ byte secret (`mix phx.gen.secret`, or any long random string).

`.env` is git-ignored and must not be committed. No secrets live in source; only `.env.example` (with placeholders) is committed.

## Run

From the repository root:

```bash
docker compose up --build
```

This builds and starts three containers (`db`, `api`, `ui`) and applies database migrations automatically on API startup. A fresh database contains only the schema and migration metadata — no seed data.

## Addresses

| Service | URL |
|---------|-----|
| UI | http://localhost:4000 |
| API | http://localhost:5080 |
| API health | http://localhost:5080/health |
| UI health | http://localhost:4000/health |
| Swagger (API, Development only) | http://localhost:5080/swagger |
| Postgres | localhost:5432 |

Set `ASPNETCORE_ENVIRONMENT=Development` in `.env` to expose Swagger UI.

## Typical flow

1. Open http://localhost:4000 → you are redirected to the login screen.
2. Create an account, then sign in.
3. Create a team (Teams), optionally add epics (Epics).
4. Open the board, create tickets, filter/search, drag cards between columns.
5. Open a ticket to edit fields, change state, add comments, or delete it.

## Tests

Backend (.NET) — service and scenario tests (auth, teams, epics, tickets, comments), plus an API smoke test:

```bash
docker run --rm -v "$PWD/api:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

UI (Phoenix) — controller and LiveView tests against a mocked API (Bypass):

```bash
docker run --rm -v "$PWD/ui:/src" -w /src \
  hexpm/elixir:1.17.3-erlang-27.1.2-debian-bookworm-20241016-slim \
  bash -lc "mix local.hex --force && mix local.rebar --force && mix deps.get && mix test"
```

## Project layout

```
.
├─ docker-compose.yml       # db + api + ui
├─ .env.example             # configuration template
├─ api/                     # .NET 8 solution (Api / Services / Data / Common / Tests)
├─ ui/                      # Phoenix app (no Ecto; HTTP client to the API)
└─ docs/                    # architecture, requirements, definition of done
```

See `docs/` for architecture and requirements, starting with `docs/ARCHITECTURE_AND_PLAN.md`.

## Known limitations / out of scope

- **Email verification** from the original brief is intentionally out of scope; authentication is simplified to sign-up + login (passwords are still hashed with Argon2id). See `docs/ARCHITECTURE_AND_PLAN.md`.
- **Automatic token refresh in the UI** is not implemented: a `/api/v1/auth/refresh` endpoint exists, but the UI does not transparently refresh the access token on expiry. With the default access-token lifetime (120 min), users simply sign in again afterwards.
- Scrum/sprints, SSO, roles/permissions, attachments, notifications, and real-time multi-user updates are out of scope.
