using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Data.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(TicketingDbContext))]
[Migration("20260706130000_AddEmailVerification")]
public partial class AddEmailVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE users ADD COLUMN is_verified boolean NOT NULL DEFAULT false;

            CREATE TABLE email_verification_tokens (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                user_id uuid NOT NULL,
                token_hash text NOT NULL,
                expires_at timestamptz NOT NULL,
                used_at timestamptz NULL,
                created_at timestamptz NOT NULL,
                CONSTRAINT pk_email_verification_tokens PRIMARY KEY (id),
                CONSTRAINT fk_email_verification_tokens_users_user_id FOREIGN KEY (user_id)
                    REFERENCES users (id) ON DELETE CASCADE
            );
            CREATE INDEX ix_email_verification_tokens_user_id ON email_verification_tokens (user_id);
            CREATE INDEX ix_email_verification_tokens_token_hash ON email_verification_tokens (token_hash);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS email_verification_tokens;
            ALTER TABLE users DROP COLUMN IF EXISTS is_verified;
            """);
    }
}
