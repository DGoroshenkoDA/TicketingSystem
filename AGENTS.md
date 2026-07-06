# Ticketing System — Development Conventions

Kanban ticket tracker. Three tiers: UI (Phoenix/LiveView) → REST API (.NET 8, EF Core) → PostgreSQL.
Everything comes up from the root with `docker compose up --build`.

Requirements and architecture live in `docs/` (start with `docs/ARCHITECTURE_AND_PLAN.md`).

## Working on a task

1. First check the requirements in `docs/` (architecture / backend / database / UI / packaging).
2. Implement the change, then verify the behavior.
3. Update docs and diagrams only after confirming the behavior is correct — never silently.

## Backend (.NET 8)

Layers and their responsibilities:

```
api/
├── Ticketing.Api/       ← HTTP endpoints, controllers, middleware, DI, JWT, error → status mapping
├── Ticketing.Services/  ← business logic (IXxxService → XxxService), validation, integrity rules
├── Ticketing.Data/      ← EF Core DbContext, entities, migrations
├── Ticketing.Common/    ← shared types, result type (ErrorOr<T> approach), enums, helpers
└── Ticketing.Tests/     ← business-flow and unit tests
```

Rules:

- **Thin controllers:** model validation → service call → map result to HTTP. No business logic in controllers.
- **Services know nothing about HTTP.** They return a result type (value or error with a code); HTTP-status mapping happens in the API layer.
- **Do not invent endpoints** — check the controllers and `docs/02_backend.md`.
- **Server-side validation is mandatory:** all enums and references are checked on the backend; client-side validation is not enough.
- **Migrations:** do not edit an already-applied migration — create a new one. Schema only through EF Core migrations, no raw SQL bypassing migrations.
- **Tests are mandatory:** all code must be covered by unit tests. In addition, write scenario tests for business flows with both positive and negative cases (e.g. sign up a user then log in; login with a wrong password; duplicate email → conflict). Code is not done until it is tested.
- C# code style: `Nullable` and `ImplicitUsings` enabled; async/await for I/O; meaningful names.

## Frontend (Phoenix/LiveView)

```
ui/lib/
├── ticketing_ui/api/          ← HttpClient, ResultParser, resource API modules
└── ticketing_ui_web/
    ├── auth.ex                ← session, token in cookie
    ├── components/            ← core_components, layouts
    ├── controllers/           ← auth pages (dead views)
    └── live/                  ← LiveView: board, ticket, teams, epics
```

Rules:

- The UI reaches the backend **only** through the REST API (`Api.*` modules); never the database directly.
- The token lives in a server-side session cookie, not in the URL and not in localStorage.
- localStorage is not used as the system of record.
- Styling — shared `core_components` and the Tailwind theme (palette in `docs/04_ui.md`); do not add inline styles that bypass the theme.
- loading / empty / success / error states are mandatory where applicable.

## Secrets and configuration

- All configuration — via env. Real secrets are not committed; the repo holds only `.env.example`.
- No hard-coded passwords or keys in source.

## Before finishing

- The affected flow works manually.
- Tests are green.
- If the schema changed — a migration is created and applied on startup.
- `docker compose up --build` from a clean checkout brings up all three containers.
