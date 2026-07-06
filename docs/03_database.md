# Database Requirements (PostgreSQL 16)

Identifiers are `uuid` (`gen_random_uuid()`). Timestamps are `timestamptz`, values in UTC. The schema is created and versioned through EF Core migrations.

## Tables

### users
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| email | text | original input |
| email_normalized | text | UNIQUE; trim + lower for case-insensitive uniqueness |
| display_name | text | non-empty after trimming |
| password_hash | text | Argon2id; plaintext never stored |
| created_at | timestamptz | |
| modified_at | timestamptz | |

### refresh_tokens
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| user_id | uuid FK → users | ON DELETE CASCADE |
| token_hash | text | hash of the refresh token; raw value never stored |
| expires_at | timestamptz | longer-lived than the access token |
| revoked_at | timestamptz NULL | set on logout or rotation |
| created_at | timestamptz | |

Index on `user_id`. A refresh token is valid only if it exists, is not expired, and `revoked_at` is null.

### teams
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| name | text | non-empty after trimming |
| name_normalized | text | UNIQUE; trim + lower |
| created_at | timestamptz | |
| modified_at | timestamptz | |

### epics
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| team_id | uuid FK → teams | set at creation, does not change |
| title | text | non-empty after trimming |
| description | text NULL | |
| created_at | timestamptz | |
| modified_at | timestamptz | |

Additional constraint: `UNIQUE (id, team_id)` — required for the composite FK from tickets.

### tickets
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| team_id | uuid FK → teams | |
| type | text | CHECK ∈ {bug, feature, fix} |
| state | text | CHECK ∈ {new, ready_for_implementation, in_progress, ready_for_acceptance, done} |
| epic_id | uuid NULL | epic from the same team (see below) |
| title | text | non-empty after trimming |
| body | text | non-empty |
| created_by | uuid FK → users | |
| created_at | timestamptz | UTC at creation |
| modified_at | timestamptz | updated only on an actual field/state change |

### comments
| Column | Type | Rules |
|--------|------|-------|
| id | uuid PK | |
| ticket_id | uuid FK → tickets | ON DELETE CASCADE |
| author_id | uuid FK → users | |
| body | text | non-empty |
| created_at | timestamptz | |

## Integrity

**Epic from the same team — at the database level.** `epics` declares `UNIQUE (id, team_id)`, and `tickets` has a composite foreign key `(epic_id, team_id) → epics (id, team_id)`. This physically prevents linking a ticket to an epic from another team, including when the ticket's team changes.

**Delete rules (→ HTTP 409):**
- A team cannot be deleted while it has tickets or epics (FK RESTRICT + service check). Cascading team deletion is not allowed.
- An epic cannot be deleted while tickets reference it (FK RESTRICT).

**Cascade:** deleting a ticket deletes its comments (FK ON DELETE CASCADE).

## Indexes

- UNIQUE on `users.email_normalized`, `teams.name_normalized`.
- Indexes for board queries: `tickets (team_id, state, modified_at DESC)`, `tickets (team_id, epic_id)`, `tickets (team_id, type)`.
- Index for title search (case-insensitive substring) — e.g., on `lower(title)`.
- `comments (ticket_id, created_at)` for chronological display.

## Initial state

After migrations are applied, a fresh database contains only the schema and migration metadata. No users, teams, epics, tickets, or comments are created by default — QA creates test data through the UI/API.
