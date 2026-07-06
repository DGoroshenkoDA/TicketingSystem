using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Data.Entities;

namespace Ticketing.Services.Teams;

public class TeamService : ITeamService
{
    private readonly TicketingDbContext _db;

    public TeamService(TicketingDbContext db) => _db = db;

    public async Task<List<TeamDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.CreatedAt,
                t.ModifiedAt,
                _db.Epics.Count(e => e.TeamId == t.Id),
                _db.Tickets.Count(tk => tk.TeamId == t.Id)))
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<TeamDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var team = await _db.Teams.FindAsync([id], ct);
        return team is null ? Error.NotFound("Team.NotFound", "Team not found.") : await ToDtoAsync(team, ct);
    }

    public async Task<ErrorOr<TeamDto>> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return Error.Validation("Team.NameRequired", "Team name is required.");
        }

        var normalized = name.ToLowerInvariant();
        if (await _db.Teams.AnyAsync(t => t.NameNormalized == normalized, ct))
        {
            return Error.Conflict("Team.NameTaken", "A team with this name already exists.");
        }

        var now = DateTime.UtcNow;
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameNormalized = normalized,
            CreatedAt = now,
            ModifiedAt = now
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        // A freshly created team is always empty.
        return new TeamDto(team.Id, team.Name, team.CreatedAt, team.ModifiedAt, 0, 0);
    }

    public async Task<ErrorOr<TeamDto>> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await _db.Teams.FindAsync([id], ct);
        if (team is null)
        {
            return Error.NotFound("Team.NotFound", "Team not found.");
        }

        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return Error.Validation("Team.NameRequired", "Team name is required.");
        }

        var normalized = name.ToLowerInvariant();
        if (await _db.Teams.AnyAsync(t => t.NameNormalized == normalized && t.Id != id, ct))
        {
            return Error.Conflict("Team.NameTaken", "A team with this name already exists.");
        }

        if (team.NameNormalized != normalized || team.Name != name)
        {
            team.Name = name;
            team.NameNormalized = normalized;
            team.ModifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(team, ct);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var team = await _db.Teams.FindAsync([id], ct);
        if (team is null)
        {
            return Error.NotFound("Team.NotFound", "Team not found.");
        }

        var hasEpics = await _db.Epics.AnyAsync(e => e.TeamId == id, ct);
        var hasTickets = await _db.Tickets.AnyAsync(t => t.TeamId == id, ct);
        if (hasEpics || hasTickets)
        {
            return Error.Conflict("Team.NotEmpty", "Cannot delete a team that still has tickets or epics.");
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<TeamDto> ToDtoAsync(Team t, CancellationToken ct)
    {
        var epicCount = await _db.Epics.CountAsync(e => e.TeamId == t.Id, ct);
        var ticketCount = await _db.Tickets.CountAsync(tk => tk.TeamId == t.Id, ct);
        return new TeamDto(t.Id, t.Name, t.CreatedAt, t.ModifiedAt, epicCount, ticketCount);
    }
}
