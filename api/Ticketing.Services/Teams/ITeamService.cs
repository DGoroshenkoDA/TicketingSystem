using ErrorOr;

namespace Ticketing.Services.Teams;

public interface ITeamService
{
    Task<List<TeamDto>> ListAsync(CancellationToken ct = default);
    Task<ErrorOr<TeamDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<TeamDto>> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<ErrorOr<TeamDto>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default);
}
