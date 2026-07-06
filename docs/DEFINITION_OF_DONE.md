# Definition of Done — status

How each acceptance criterion is met and how to verify it. Run the full check from a clean checkout: `docker compose down -v` then `docker compose up --build`.

| Criterion | Status | Where / how to verify |
|-----------|--------|-----------------------|
| A user can sign up and log in | Done | Sign-up + login screens; `POST /api/v1/auth/signup` and `/login`. (Email verification is intentionally out of scope — see below.) |
| Teams and epics managed via UI, persisted in DB | Done | `/teams` and `/epics` LiveViews → Teams/Epics API → Postgres. |
| A verified user can create, view, edit, delete tickets | Done | Board + ticket modal → Tickets API. |
| Comments with author and timestamp | Done | Ticket modal comments section; oldest-first. |
| Board shows tickets in the correct state columns | Done | `/board`, five workflow columns. |
| Dragging a ticket updates the server and survives refresh | Done | DnD hook → `PATCH /api/v1/tickets/{id}/state`; reload the page to confirm. |
| `docker compose up --build` from a clean checkout | Done | Root `docker-compose.yml`; migrations auto-applied on API start. |
| No hard-coded passwords or committed secrets | Done | Config via env; only `.env.example` committed; `.env` git-ignored. |
| Fresh database has schema + migration metadata only | Done | No seed data; `InitialSchema` migration only. |
| QA can create all data through the UI/API | Done | All CRUD via API; no manual DB edits required. |

## Integrity rules (HTTP 409 / validation)

- Delete a team with tickets or epics → 409; delete button disabled in the UI.
- Delete an epic referenced by tickets → 409; delete button disabled.
- Ticket epic must belong to the ticket's team → enforced by a composite FK in the DB and validated in the API.
- `modified_at` advances only on a real ticket field/state change; adding a comment does not change it.

## Tests

- **Backend (.NET / xUnit):** password hasher, token service, and scenario tests for auth, teams, epics, tickets, and comments (positive + negative), plus an API `/health` smoke test.
- **UI (Phoenix / ExUnit + Bypass):** auth controller flow, team management LiveView, and board LiveView (render, create, drag move success/failure, comments).

Run both suites with the commands in the root `README.md`.

## Security check

- Passwords hashed with Argon2id; never stored in plaintext.
- Authenticated endpoints protected by a JWT bearer fallback policy (only sign-up, login, refresh, and `/health` are public).
- Tokens are not placed in URLs; the UI keeps them in a server-side session cookie.
- No secrets in source control; SMTP/secret material is not required (email verification is out of scope).

## Deviations from the original brief

- **Email verification** (SMTP, 24h single-use tokens, verification/resend screens) is intentionally out of scope; auth is sign-up + login only.
- **UI does not auto-refresh** the access token on expiry, though a refresh endpoint exists.
