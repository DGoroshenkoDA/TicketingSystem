using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Data.Entities;

namespace Ticketing.Services.Epics;

public class EpicService : IEpicService
{
    private readonly TicketingDbContext _db;

    public EpicService(TicketingDbContext db) => _db = db;

    public async Task<List<EpicDto>> ListAsync(Guid teamId, CancellationToken ct = default)
    {
        return await _db.Epics
            .Where(e => e.TeamId == teamId)
            .OrderBy(e => e.Title)
            .Select(e => new EpicDto(
                e.Id,
                e.TeamId,
                e.Title,
                e.Description,
                e.CreatedAt,
                e.ModifiedAt,
                _db.Tickets.Count(t => t.EpicId == e.Id)))
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<EpicDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var epic = await _db.Epics.FindAsync([id], ct);
        return epic is null ? Error.NotFound("Epic.NotFound", "Epic not found.") : await ToDtoAsync(epic, ct);
    }

    public async Task<ErrorOr<EpicDto>> CreateAsync(CreateEpicRequest request, CancellationToken ct = default)
    {
        var title = request.Title.Trim();
        if (title.Length == 0)
        {
            return Error.Validation("Epic.TitleRequired", "Epic title is required.");
        }

        if (!await _db.Teams.AnyAsync(t => t.Id == request.TeamId, ct))
        {
            return Error.NotFound("Team.NotFound", "The selected team does not exist.");
        }

        var now = DateTime.UtcNow;
        var epic = new Epic
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            Title = title,
            Description = NormalizeDescription(request.Description),
            CreatedAt = now,
            ModifiedAt = now
        };

        _db.Epics.Add(epic);
        await _db.SaveChangesAsync(ct);
        // A freshly created epic has no tickets referencing it yet.
        return new EpicDto(epic.Id, epic.TeamId, epic.Title, epic.Description, epic.CreatedAt, epic.ModifiedAt, 0);
    }

    public async Task<ErrorOr<EpicDto>> UpdateAsync(Guid id, UpdateEpicRequest request, CancellationToken ct = default)
    {
        var epic = await _db.Epics.FindAsync([id], ct);
        if (epic is null)
        {
            return Error.NotFound("Epic.NotFound", "Epic not found.");
        }

        var title = request.Title.Trim();
        if (title.Length == 0)
        {
            return Error.Validation("Epic.TitleRequired", "Epic title is required.");
        }

        var description = NormalizeDescription(request.Description);

        // The team is fixed after creation and is intentionally not updated here.
        if (epic.Title != title || epic.Description != description)
        {
            epic.Title = title;
            epic.Description = description;
            epic.ModifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(epic, ct);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var epic = await _db.Epics.FindAsync([id], ct);
        if (epic is null)
        {
            return Error.NotFound("Epic.NotFound", "Epic not found.");
        }

        if (await _db.Tickets.AnyAsync(t => t.EpicId == id, ct))
        {
            return Error.Conflict("Epic.Referenced", "Cannot delete an epic that is referenced by tickets.");
        }

        _db.Epics.Remove(epic);
        await _db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private async Task<EpicDto> ToDtoAsync(Epic e, CancellationToken ct)
    {
        var ticketCount = await _db.Tickets.CountAsync(t => t.EpicId == e.Id, ct);
        return new EpicDto(e.Id, e.TeamId, e.Title, e.Description, e.CreatedAt, e.ModifiedAt, ticketCount);
    }
}
