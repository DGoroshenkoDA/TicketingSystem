using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Data.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(TicketingDbContext))]
[Migration("20260707130000_AddTicketHistory")]
public partial class AddTicketHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE ticket_history (
                id uuid NOT NULL DEFAULT gen_random_uuid(),
                ticket_id uuid NOT NULL,
                changed_by uuid NOT NULL,
                changed_at timestamptz NOT NULL,
                field text NOT NULL,
                old_value text NULL,
                new_value text NULL,
                CONSTRAINT pk_ticket_history PRIMARY KEY (id),
                CONSTRAINT fk_ticket_history_tickets_ticket_id FOREIGN KEY (ticket_id)
                    REFERENCES tickets (id) ON DELETE CASCADE,
                CONSTRAINT fk_ticket_history_users_changed_by FOREIGN KEY (changed_by)
                    REFERENCES users (id) ON DELETE RESTRICT
            );
            CREATE INDEX ix_ticket_history_ticket_id_changed_at ON ticket_history (ticket_id, changed_at DESC);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS ticket_history;
            """);
    }
}
