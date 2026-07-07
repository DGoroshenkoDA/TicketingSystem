using ErrorOr;

namespace Ticketing.Services.Tickets;

public interface ITicketService
{
    Task<ErrorOr<List<TicketDto>>> ListAsync(TicketQuery query, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> CreateAsync(CreateTicketRequest request, Guid createdBy, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> UpdateAsync(Guid id, UpdateTicketRequest request, Guid changedBy, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> UpdateStateAsync(Guid id, UpdateTicketStateRequest request, Guid changedBy, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<List<TicketHistoryDto>>> GetHistoryAsync(Guid ticketId, CancellationToken ct = default);
}
