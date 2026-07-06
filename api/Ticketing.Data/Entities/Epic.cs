namespace Ticketing.Data.Entities;

public class Epic
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public Team? Team { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
