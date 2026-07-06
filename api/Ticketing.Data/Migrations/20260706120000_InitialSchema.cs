using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Data.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(TicketingDbContext))]
[Migration("20260706120000_InitialSchema")]
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE users (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                email text NOT NULL,
                email_normalized text NOT NULL,
                display_name text NOT NULL,
                password_hash text NOT NULL,
                created_at timestamptz NOT NULL,
                modified_at timestamptz NOT NULL,
                CONSTRAINT pk_users PRIMARY KEY (id)
            );
            CREATE UNIQUE INDEX ix_users_email_normalized ON users (email_normalized);

            CREATE TABLE refresh_tokens (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                user_id uuid NOT NULL,
                token_hash text NOT NULL,
                expires_at timestamptz NOT NULL,
                revoked_at timestamptz NULL,
                created_at timestamptz NOT NULL,
                CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
                CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id)
                    REFERENCES users (id) ON DELETE CASCADE
            );
            CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);

            CREATE TABLE teams (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                name text NOT NULL,
                name_normalized text NOT NULL,
                created_at timestamptz NOT NULL,
                modified_at timestamptz NOT NULL,
                CONSTRAINT pk_teams PRIMARY KEY (id)
            );
            CREATE UNIQUE INDEX ix_teams_name_normalized ON teams (name_normalized);

            CREATE TABLE epics (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                team_id uuid NOT NULL,
                title text NOT NULL,
                description text NULL,
                created_at timestamptz NOT NULL,
                modified_at timestamptz NOT NULL,
                CONSTRAINT pk_epics PRIMARY KEY (id),
                CONSTRAINT ak_epics_id_team_id UNIQUE (id, team_id),
                CONSTRAINT fk_epics_teams_team_id FOREIGN KEY (team_id)
                    REFERENCES teams (id) ON DELETE RESTRICT
            );
            CREATE INDEX ix_epics_team_id ON epics (team_id);

            CREATE TABLE tickets (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                team_id uuid NOT NULL,
                type text NOT NULL,
                state text NOT NULL,
                epic_id uuid NULL,
                title text NOT NULL,
                body text NOT NULL,
                created_by uuid NOT NULL,
                created_at timestamptz NOT NULL,
                modified_at timestamptz NOT NULL,
                CONSTRAINT pk_tickets PRIMARY KEY (id),
                CONSTRAINT ck_tickets_type CHECK (type IN ('bug','feature','fix')),
                CONSTRAINT ck_tickets_state CHECK (state IN ('new','ready_for_implementation','in_progress','ready_for_acceptance','done')),
                CONSTRAINT fk_tickets_teams_team_id FOREIGN KEY (team_id)
                    REFERENCES teams (id) ON DELETE RESTRICT,
                CONSTRAINT fk_tickets_users_created_by FOREIGN KEY (created_by)
                    REFERENCES users (id) ON DELETE RESTRICT,
                CONSTRAINT fk_tickets_epics_epic_id_team_id FOREIGN KEY (epic_id, team_id)
                    REFERENCES epics (id, team_id) ON DELETE RESTRICT
            );
            CREATE INDEX ix_tickets_team_id_state_modified_at ON tickets (team_id, state, modified_at DESC);
            CREATE INDEX ix_tickets_team_id_epic_id ON tickets (team_id, epic_id);
            CREATE INDEX ix_tickets_team_id_type ON tickets (team_id, type);
            CREATE INDEX ix_tickets_epic_id ON tickets (epic_id);
            CREATE INDEX ix_tickets_title_lower ON tickets (lower(title));

            CREATE TABLE comments (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                ticket_id uuid NOT NULL,
                author_id uuid NOT NULL,
                body text NOT NULL,
                created_at timestamptz NOT NULL,
                CONSTRAINT pk_comments PRIMARY KEY (id),
                CONSTRAINT fk_comments_tickets_ticket_id FOREIGN KEY (ticket_id)
                    REFERENCES tickets (id) ON DELETE CASCADE,
                CONSTRAINT fk_comments_users_author_id FOREIGN KEY (author_id)
                    REFERENCES users (id) ON DELETE RESTRICT
            );
            CREATE INDEX ix_comments_ticket_id_created_at ON comments (ticket_id, created_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS comments;
            DROP TABLE IF EXISTS tickets;
            DROP TABLE IF EXISTS epics;
            DROP TABLE IF EXISTS teams;
            DROP TABLE IF EXISTS refresh_tokens;
            DROP TABLE IF EXISTS users;
            """);
    }
}
