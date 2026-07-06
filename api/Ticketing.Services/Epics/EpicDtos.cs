namespace Ticketing.Services.Epics;

public record EpicDto(
    Guid Id,
    Guid TeamId,
    string Title,
    string? Description,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    int TicketCount)
{
    // Delete is only allowed when no tickets reference the epic.
    public bool CanDelete => TicketCount == 0;
}

public record CreateEpicRequest(Guid TeamId, string Title, string? Description);

public record UpdateEpicRequest(string Title, string? Description);
