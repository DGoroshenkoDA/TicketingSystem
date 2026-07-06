using ErrorOr;

namespace Ticketing.Services.Epics;

public interface IEpicService
{
    Task<List<EpicDto>> ListAsync(Guid teamId, CancellationToken ct = default);
    Task<ErrorOr<EpicDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<EpicDto>> CreateAsync(CreateEpicRequest request, CancellationToken ct = default);
    Task<ErrorOr<EpicDto>> UpdateAsync(Guid id, UpdateEpicRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default);
}
