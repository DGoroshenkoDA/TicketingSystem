# Ticketing System — Overview and Plan

Kanban ticket tracker. Three-tier architecture: **UI (Phoenix/LiveView) → REST API (.NET 8) → PostgreSQL**.
The whole stack comes up from the repository root with a single `docker compose up --build`.

## Documents

| File | Contents |
|------|----------|
| [`01_architecture.md`](01_architecture.md) | Overall architecture: tiers, topology, data flows, repository layout. |
| [`02_backend.md`](02_backend.md) | Backend requirements: layers, API, authentication, validation, errors. |
| [`03_database.md`](03_database.md) | Database requirements: schema, constraints, integrity, migrations. |
| [`04_ui.md`](04_ui.md) | UI requirements: screens, Kanban board, drag & drop, styling. |
| [`05_packaging_deployment.md`](05_packaging_deployment.md) | Packaging and deployment: Docker Compose, images, config, startup. |
| [`../AGENTS.md`](../AGENTS.md) | Development conventions for the project. |

## Key decisions

| Tier | Technology |
|------|-----------|
| Presentation (UI) | Phoenix 1.7 + LiveView 1.0, Tailwind, esbuild, heroicons; HTTP client to the API |
| Application / API | .NET 8, ASP.NET Core Web API, EF Core (Npgsql) |
| Persistence | PostgreSQL 16, EF Core migrations |
| Orchestration | Docker Compose (db + api + ui) |

## Implementation plan (phases)

**Phase 0 — Skeleton.** Repository, `docker-compose.yml`, `api` and `ui` skeletons, both Dockerfiles. Goal: `docker compose up --build` brings up 3 containers, `/health` is green, UI serves a page.

**Phase 1 — Database and schema.** EF Core entities + first migration (users, teams, epics, tickets, comments) with all constraints. Migrations auto-applied on API startup. Clean database with no seed data.

**Phase 2 — Authentication (API).** Signup (email, displayName, password + confirmation; Argon2id; email uniqueness trim + case-insensitive), login (email + password → JWT), logout, guard on all business endpoints.

**Phase 3 — Authentication (UI).** HTTP client, result parser, session module with the token in a cookie. Profile creation and login screens.

**Phase 4 — Teams + Epics (API + UI).** CRUD, integrity rules and 409, disabled states for delete buttons, management screens.

**Phase 5 — Tickets (API + UI).** CRUD, server-side validation of enums/references, `modified_at` logic, clearing the epic when the team changes, delete confirmation, comment cascade.

**Phase 6 — Kanban board.** LiveView: 5 columns, team selector, cards, type/epic filters + search, sorting by modified_at DESC.

**Phase 7 — Drag & drop.** JS hook + `PATCH state`, optimistic move, rollback + error on failure, verification with 100+ tickets.

**Phase 8 — Comments.** Adding, chronological display (oldest first), immutability, no effect on the ticket's `modified_at`.

**Phase 9 — Tests, README, wrap-up.** Backend business flow + UI/API flow, README (prereqs/config/startup), verify "no secrets in source", run Definition of Done on a clean checkout.

## Definition of Done

| Criterion | Where it is covered |
|-----------|---------------------|
| A user can create a profile and log in | Phases 2–3 |
| Teams and epics managed via UI, persisted in DB | Phase 4 |
| Ticket CRUD by an authenticated user | Phase 5 |
| Comments with author and timestamp | Phase 8 |
| Board shows tickets in the correct state columns | Phase 6 |
| Drag → server updated, correct after refresh | Phase 7 |
| `docker compose up --build` from a clean checkout | Phase 0 (verified in Phase 9) |
| No hard-coded passwords or committed secrets | env config, Phases 2/9 |
| Fresh database: schema and migration metadata only | Phase 1 |
| QA creates data through the UI/API | All phases (no seeds) |

## Deviation from the original hackathon spec

The original requirements (`requirements/…docx`, sections 3 and 10) mandate email verification: a v