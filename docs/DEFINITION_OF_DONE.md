# Definition of Done — status

How each acceptance criterion is met and how to verify it. Run the full check from a clean checkout: `docker compose down -v` then `docker compose up --build`.

| Criterion | Status | Where / how to verify |
|-----------|--------|-----------------------|
| A user can sign up, verify their email, and log in | Done | Sign-up + login screens; `POST /api/v1/auth/signup` sends a verification email via SMTP (`relay1.dataart.com`), `GET /api/v1/auth/verify?token=` confirms, `POST /api/v1/auth/resend-verification` re-sends. Unverified users are blocked at login when `APP_REQUIRE_EMAIL_VERIFICATION=true` (default). |
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

- **Backend (.NET / xUnit):** password hasher, token service, and scenario tests for auth (incl. email verification single-use + expiry, resend no-op for verified users), teams, epics, tickets, and comments (positive + negative), plus ticket-list filter validation and an API `/health` smoke test.
- **UI (Phoenix / ExUnit + Bypass):** auth controller flow (login, logout, verify, resend, session refresh, auth redirects), profile controller, team/epic management LiveViews, board LiveView (render, create, drag move success/failure, comments, filters/search, team switch), and HttpClient/ResultParser units.

Run both suites with the commands in the root `README.md`.

## Security check

- Passwords hashed with Argon2id; never stored in plaintext.
- Authenticated endpoints protected by a JWT bearer fallback policy (only sign-up, login, refresh, email verify, resend-verification, and `/health` are public).
- Tokens are not placed in URLs; the UI keeps them in a server-side session cookie.
- No secrets in source control; `.env` is git-ignored (only `.env.example` is tracked). SMTP host and all secrets are supplied via env.
- Email verification: tokens are single-use and expire after 24h; only the hash is stored; the raw verification link is logged at Debug (and at Warning only if the SMTP send fails), never at Information.

## Deviations from the original brief

- None outstanding. Email verification and UI token auto-refresh — previously deferred — are now implemented. `APP_REQUIRE_EMAIL_VERIFICATION` can be set to `false` for local testing without SMTP, but ships as `true`.
