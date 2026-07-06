using ErrorOr;

namespace Ticketing.Services.Tickets;

public interface ITicketService
{
    Task<List<TicketDto>> ListAsync(TicketQuery query, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> CreateAsync(CreateTicketRequest request, Guid createdBy, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct = default);
    Task<ErrorOr<TicketDto>> UpdateStateAsync(Guid id, UpdateTicketStateRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default);
}
