namespace Ticketing.Data.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }

    // Canonical string values, validated by CHECK constraints and the service layer:
    // type  in (bug, feature, fix)
    // state in (new, ready_for_implementation, in_progress, ready_for_acceptance, done)
    public string Type { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public Guid? EpicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public Team? Team { get; set; }
    public Epic? Epic { get; set; }
    public User? Creator { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
