namespace Ticketing.Services.Tickets;

public record TicketDto(
    Guid Id,
    Guid TeamId,
    string Type,
    string State,
    Guid? EpicId,
    string? EpicTitle,
    string Title,
    string Body,
    Guid CreatedBy,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime ModifiedAt);

public record CreateTicketRequest(Guid TeamId, string Type, Guid? EpicId, string Title, string Body);

public record UpdateTicketRequest(Guid TeamId, string Type, Guid? EpicId, string Title, string Body, string State);

public record UpdateTicketStateRequest(string State);

public record TicketQuery(Guid TeamId, string? Type, Guid? EpicId, string? Search);
