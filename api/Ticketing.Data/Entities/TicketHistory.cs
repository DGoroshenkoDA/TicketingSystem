namespace Ticketing.Data.Entities;

public class TicketHistory
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }

    // The changed field name: one of type, state, title, body, epic, team.
    public string Field { get; set; } = string.Empty;

    // Human-readable, immutable values captured at change time (nullable when absent/cleared).
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public Ticket? Ticket { get; set; }
    public User? ChangedByUser { get; set; }
}
