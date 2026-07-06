namespace Ticketing.Data.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameNormalized { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public ICollection<Epic> Epics { get; set; } = new List<Epic>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
