# Overall Architecture

## Tiers

Three logically and physically separated tiers, each its own container:

| Tier | Role | Technology |
|------|------|-----------|
| Presentation | UI, screen rendering, drag & drop | Phoenix 1.7 + LiveView 1.0 |
| Application / API | Business logic, validation, sole owner of data | .NET 8 ASP.NET Core Web API + EF Core |
| Persistence | Storage | PostgreSQL 16 |

Principles:

- The UI **never** accesses the database directly — only the REST API over HTTP.
- The API is the sole owner of the schema and data; it applies migrations on startup.
- Browser localStorage is not used as the system of record (at most, UI state such as the active filter).

## Topology and data flow

```
Browser (Chrome / Edge / Firefox)
        │  HTTP / WebSocket
        ▼
┌────────────────────────────┐        ┌───────────────────────────┐        ┌────────────────┐
│  ui  (Phoenix + LiveView)   │  HTTP  │  api  (.NET 8 REST API)    │  SQL   │  db (Postgres)  │
│  - LiveView screens         │ ─────► │  - Controllers (HTTP)      │ ─────► │  - schema via   │
│  - HTTP client to the API   │  JSON  │  - Services (business)     │        │    EF migrations│
│  - session cookie + token   │ ◄───── │  - Data (EF Core DbContext)│ ◄───── │  - no seed data │
└────────────────────────────┘        │  - JWT, validation, errors │        └────────────────┘
                                       └────────────────────────────┘
```

## Authentication (end-to-end flow)

- A user creates a profile and logs in through the API (details in `02_backend.md`).
- Login returns a JWT (bearer token).
- The UI stores the token in a **server-side session cookie** (not in the URL, not in localStorage) and adds an `Authorization: Bearer …` header to every API call.
- All business screens and endpoints require authentication; only signup, login, static assets, and the health endpoint are public.

## Meeting the "three tiers" requirement

- "Backend exposing an HTTP API" — a separate .NET REST API.
- "Clear separation of tiers" — three separate containers, UI has no database access.
- Sessions/tokens are not in URLs; data flows only through the API and lives in the RDBMS.

Note: the LiveView UI provides single-page behavior (navigation without full reloads, live updates). This is a deliberate choice for the presentation tier.

## Repository layout

```
TicketingSystem/
├─ docker-compose.yml            # db + api + ui; single up --build
├─ .env.example                  # config (JWT secret, DB) — no real secrets
├─ README.md                     # prerequisites, config, startup
├─ AGENTS.md                     # development conventions
├─ docs/                         # these documents
├─ api/                          # .NET 8 solution
│  ├─ Dockerfile                 # multi-stage: sdk build → aspnet runtime
│  ├─ Ticketing.sln
│  ├─ Ticketing.Api/             # controllers, auth, Program.cs, DI, middleware
│  ├─ Ticketing.Services/        # business logic, validation, DTOs
│  ├─ Ticketing.Data/            # EF Core DbContext, entities, migrations
│  ├─ Ticketing.Common/          # shared types, results, enums
│  └─ Ticketing.Tests/           # backend business flow
└─ ui/                           # Phoenix app (no Ecto)
   ├─ Dockerfile                 # multi-stage: mix release + assets.deploy
   ├─ mix.exs                    # app :ticketing_ui
   ├─ assets/                    # Tailwind + app.js + hooks (drag & drop)
   └─ lib/
      ├─ ticketing_ui/
      │  └─ api/                 # HttpClient, ResultParser, resource API modules
      └─ ticketing_ui_web/
         ├─ auth.ex              # session/plug, token in cookie
         ├─ components/          # core_components, layouts
         ├─ controllers/         # auth pages (signup / login) as dead views
         └─ live/                # LiveView: board, ticket, teams, epics
```

## Non-functional requirements (cross-cutting)

- **Security:** protect authenticated endpoints, hash passwords, validate input, no secrets in source.
- **Reliability:** a browser refresh or container restart does not lose data (a consequence of "API is the system of record").
- **Usability:** loading / empty / success / error states where applicable.
- **Compatibility:** current desktop Chrome, Edge, Firefox.
- **Maintainability:** README with prerequisites, config, startup commands.
- **Testing:** all code is covered by unit tests; scenario tests exercise end-to-end business flows with both positive and negative cases (e.g. sign up then log in; login with wrong password; duplicate email). At least one UI/API flow is covered on the frontend side.
