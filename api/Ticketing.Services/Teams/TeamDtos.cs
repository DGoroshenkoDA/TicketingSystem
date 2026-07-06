namespace Ticketing.Services.Teams;

public record TeamDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    int EpicCount,
    int TicketCount)
{
    // Delete is only allowed for an empty team (no epics and no tickets).
    public bool CanDelete => EpicCount == 0 && TicketCount == 0;
}

public record CreateTeamRequest(string Name);

public record UpdateTeamRequest(string Name);
