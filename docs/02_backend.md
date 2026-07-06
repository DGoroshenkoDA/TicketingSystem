# Backend Requirements (.NET 8 REST API)

## Stack

- .NET 8 (LTS), ASP.NET Core Web API.
- EF Core + Npgsql (PostgreSQL).
- FluentValidation — input model validation.
- Operation results via a result type (`ErrorOr<T>` approach): a service returns either a value or an error with a code; the controller maps it to an HTTP status.
- JWT bearer authentication.
- Swagger/OpenAPI for documentation and manual checks.
- Structured logging.

## Layers

| Project | Responsibility |
|---------|----------------|
| `Ticketing.Api` | HTTP endpoints, controllers, middleware, DI, error-to-status mapping, JWT. |
| `Ticketing.Services` | Business logic, validation, integrity rules, DTOs. |
| `Ticketing.Data` | EF Core `DbContext`, entities, migrations, data access. |
| `Ticketing.Common` | Shared types, result type, enums, helpers. |
| `Ticketing.Tests` | Business-flow tests. |

Layer rules: controllers are thin (model validation → service call → map result to HTTP); all business logic and integrity checks live in services; services know nothing about HTTP.

## Authentication

Simplified model (no email verification, no SMTP, no confirmation tokens).

**Create profile** — `POST /api/v1/auth/signup` **[public]**
Fields: `email`, `displayName`, `password`, `passwordConfirm`.
Rules:
- `email` — trimmed, compared case-insensitively, unique.
- `password` — at least 8 characters, matches `passwordConfirm`.
- Password is hashed with **Argon2id**; the plaintext is never stored.
- `displayName` — non-empty after trimming.

**Login** — `POST /api/v1/auth/login` **[public]**
Fields: `email`, `password`. On success returns an **access token** (short-lived JWT) and a **refresh token** (longer-lived), plus profile data.

**Refresh** — `POST /api/v1/auth/refresh` **[public]**
Exchanges a valid refresh token for a new access token (and, optionally, a rotated refresh token). Invalid/expired refresh tokens return 401.

**Logout** — `POST /api/v1/auth/logout` — invalidates the current refresh token.

All other business endpoints require a valid access token. The token is sent in the `Authorization: Bearer …` header, never in the URL. The UI stores tokens in its server-side session cookie and refreshes transparently.

## REST API

Response format — envelope `{ "success": true, "data": … }`; errors — `{ "success": false, "detail"/"message": … }` with the correct HTTP status. Identifiers are UUIDs. Timestamps are ISO-8601 in UTC.

**Auth**
- `POST /api/v1/auth/signup` **[public]**
- `POST /api/v1/auth/login` **[public]**
- `POST /api/v1/auth/refresh` **[public]**
- `POST /api/v1/auth/logout`

**Teams**
- `GET /api/v1/teams`
- `POST /api/v1/teams`
- `PUT /api/v1/teams/{id}`
- `DELETE /api/v1/teams/{id}` — 409 if the team has tickets or epics.

**Epics**
- `GET /api/v1/epics?teamId=…`
- `POST /api/v1/epics` — the team is set at creation.
- `PUT /api/v1/epics/{id}` — the team does not change.
- `DELETE /api/v1/epics/{id}` — 409 if tickets reference the epic.

**Tickets**
- `GET /api/v1/tickets?teamId=…&type=…&epicId=…&search=…` — filters combined with AND; `search` is a case-insensitive substring over title.
- `GET /api/v1/tickets/{id}`
- `POST /api/v1/tickets`
- `PUT /api/v1/tickets/{id}`
- `PATCH /api/v1/tickets/{id}/state` — change state (drag & drop), persisted immediately.
- `DELETE /api/v1/tickets/{id}` — cascades to delete comments.

**Comments**
- `GET /api/v1/tickets/{id}/comments` — chronological, oldest first.
- `POST /api/v1/tickets/{id}/comments`

**Health** — `GET /health` **[public]**.

## Business rules (server side)

- Validate all enum values (`type`, `state`) and references (team, epic, ticket) — client-side validation alone is insufficient.
- A ticket may reference only an epic from its own team; the backend rejects mismatches.
- When a ticket's team changes, an incompatible epic is not allowed (the UI clears the epic, the backend enforces it).
- A ticket's `modified_at` is updated only on an actual field/state change; saving unchanged values does not advance the timestamp; adding a comment does not advance it.
- Deleting a ticket requires explicit confirmation in the UI; the backend cascades to comments.
- Comments are immutable in the mandatory scope.
- Ticket `body` accepts plain text or Markdown; rich-text editing is not required and no application-level max length is enforced.
- No membership/ownership model: every authenticated user can view and manage all teams, epics, tickets, and comments.
- Concurrency: concurrent-edit conflict detection is not required; the last successful write wins.

## HTTP codes

| Situation 