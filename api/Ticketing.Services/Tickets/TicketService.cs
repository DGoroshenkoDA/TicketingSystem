using System.Linq.Expressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Data.Entities;

namespace Ticketing.Services.Tickets;

public class TicketService : ITicketService
{
    private readonly TicketingDbContext _db;

    public TicketService(TicketingDbContext db) => _db = db;

    private static readonly Expression<Func<Ticket, TicketDto>> Projection = t => new TicketDto(
        t.Id,
        t.TeamId,
        t.Type,
        t.State,
        t.EpicId,
        t.Epic != null ? t.Epic.Title : null,
        t.Title,
        t.Body,
        t.CreatedBy,
        t.Creator != null ? t.Creator.DisplayName : null,
        t.CreatedAt,
        t.ModifiedAt);

    private static readonly Expression<Func<TicketHistory, TicketHistoryDto>> HistoryProjection = h => new TicketHistoryDto(
        h.Id,
        h.Field,
        h.OldValue,
        h.NewValue,
        h.ChangedByUser != null ? h.ChangedByUser.DisplayName : null,
        h.ChangedAt);

    public async Task<ErrorOr<List<TicketDto>>> ListAsync(TicketQuery query, CancellationToken ct = default)
    {
        var q = _db.Tickets.Where(t => t.TeamId == query.TeamId);

        // An absent/empty type means "no filter"; a non-empty but unknown value is a bad request
        // and must not be silently ignored (which would return every ticket).
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!TicketEnums.IsValidType(query.Type))
            {
                return Error.Validation("Ticket.InvalidType", "Ticket type must be one of: bug, feature, fix.");
            }

            q = q.Where(t => t.Type == query.Type);
        }

        if (query.EpicId is { } epicId)
        {
            q = q.Where(t => t.EpicId == epicId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(term));
        }

        return await q
            .OrderByDescending(t => t.ModifiedAt)
            .Select(Projection)
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<TicketDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.Tickets.Where(t => t.Id == id).Select(Projection).FirstOrDefaultAsync(ct);
        return dto is null ? Error.NotFound("Ticket.NotFound", "Ticket not found.") : dto;
    }

    public async Task<ErrorOr<TicketDto>> CreateAsync(CreateTicketRequest request, Guid createdBy, CancellationToken ct = default)
    {
        if (!TicketEnums.IsValidType(request.Type))
        {
            return Error.Validation("Ticket.InvalidType", "Ticket type must be one of: bug, feature, fix.");
        }

        var title = request.Title.Trim();
        if (title.Length == 0)
        {
            return Error.Validation("Ticket.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Error.Validation("Ticket.BodyRequired", "Body is required.");
        }

        if (!await _db.Teams.AnyAsync(t => t.Id == request.TeamId, ct))
        {
            return Error.NotFound("Team.NotFound", "The selected team does not exist.");
        }

        var epicError = await ValidateEpicAsync(request.TeamId, request.EpicId, ct);
        if (epicError is { } err)
        {
            return err;
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            Type = request.Type,
            State = TicketEnums.DefaultState,
            EpicId = request.EpicId,
            Title = title,
            Body = request.Body,
            CreatedBy = createdBy,
            CreatedAt = now,
            ModifiedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return (await _db.Tickets.Where(t => t.Id == ticket.Id).Select(Projection).FirstAsync(ct));
    }

    public async Task<ErrorOr<TicketDto>> UpdateAsync(Guid id, UpdateTicketRequest request, Guid changedBy, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        if (!TicketEnums.IsValidType(request.Type))
        {
            return Error.Validation("Ticket.InvalidType", "Ticket type must be one of: bug, feature, fix.");
        }

        if (!TicketEnums.IsValidState(request.State))
        {
            return Error.Validation("Ticket.InvalidState", "Ticket state is not a valid workflow state.");
        }

        var title = request.Title.Trim();
        if (title.Length == 0)
        {
            return Error.Validation("Ticket.TitleRequired", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Error.Validation("Ticket.BodyRequired", "Body is required.");
        }

        if (!await _db.Teams.AnyAsync(t => t.Id == request.TeamId, ct))
        {
            return Error.NotFound("Team.NotFound", "The selected team does not exist.");
        }

        // Enforce that the epic (if any) belongs to the ticket's (possibly new) team.
        var epicError = await ValidateEpicAsync(request.TeamId, request.EpicId, ct);
        if (epicError is { } err)
        {
            return err;
        }

        // Per-field change detection: drives both the modified_at guard and the audit log.
        var teamChanged = ticket.TeamId != request.TeamId;
        var typeChanged = ticket.Type != request.Type;
        var stateChanged = ticket.State != request.State;
        var epicChanged = ticket.EpicId != request.EpicId;
        var titleChanged = ticket.Title != title;
        var bodyChanged = ticket.Body != request.Body;

        var changed = teamChanged || typeChanged || stateChanged || epicChanged || titleChanged || bodyChanged;

        if (changed)
        {
            var now = DateTime.UtcNow;
            var history = new List<TicketHistory>();

            void Log(string field, string? oldValue, string? newValue) =>
                history.Add(new TicketHistory
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    ChangedBy = changedBy,
                    ChangedAt = now,
                    Field = field,
                    OldValue = oldValue,
                    NewValue = newValue
                });

            // Simple text/enum fields store their raw values.
            if (titleChanged) Log("title", ticket.Title, title);
            if (bodyChanged) Log("body", ticket.Body, request.Body);
            if (typeChanged) Log("type", ticket.Type, request.Type);
            if (stateChanged) Log("state", ticket.State, request.State);

            // Epic/team store human-readable names resolved with one batched lookup each.
            if (epicChanged)
            {
                var (oldEpic, newEpic) = await ResolveEpicValuesAsync(ticket.EpicId, request.EpicId, ct);
                Log("epic", oldEpic, newEpic);
            }

            if (teamChanged)
            {
                var (oldTeam, newTeam) = await ResolveTeamValuesAsync(ticket.TeamId, request.TeamId, ct);
                Log("team", oldTeam, newTeam);
            }

            ticket.TeamId = request.TeamId;
            ticket.Type = request.Type;
            ticket.State = request.State;
            ticket.EpicId = request.EpicId;
            ticket.Title = title;
            ticket.Body = request.Body;
            ticket.ModifiedAt = now;
            _db.TicketHistory.AddRange(history);
            await _db.SaveChangesAsync(ct);
        }

        return await _db.Tickets.Where(t => t.Id == id).Select(Projection).FirstAsync(ct);
    }

    public async Task<ErrorOr<TicketDto>> UpdateStateAsync(Guid id, UpdateTicketStateRequest request, Guid changedBy, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        if (!TicketEnums.IsValidState(request.State))
        {
            return Error.Validation("Ticket.InvalidState", "Ticket state is not a valid workflow state.");
        }

        if (ticket.State != request.State)
        {
            var now = DateTime.UtcNow;
            _db.TicketHistory.Add(new TicketHistory
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ChangedBy = changedBy,
                ChangedAt = now,
                Field = "state",
                OldValue = ticket.State,
                NewValue = request.State
            });
            ticket.State = request.State;
            ticket.ModifiedAt = now;
            await _db.SaveChangesAsync(ct);
        }

        return await _db.Tickets.Where(t => t.Id == id).Select(Projection).FirstAsync(ct);
    }

    public async Task<ErrorOr<List<TicketHistoryDto>>> GetHistoryAsync(Guid ticketId, CancellationToken ct = default)
    {
        if (!await _db.Tickets.AnyAsync(t => t.Id == ticketId, ct))
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        return await _db.TicketHistory
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(HistoryProjection)
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<Success>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        // Comments are removed by the database cascade (fk_comments_tickets ON DELETE CASCADE).
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<Error?> ValidateEpicAsync(Guid teamId, Guid? epicId, CancellationToken ct)
    {
        if (epicId is null)
        {
            return null;
        }

        var epic = await _db.Epics.FirstOrDefaultAsync(e => e.Id == epicId.Value, ct);
        if (epic is null)
        {
            return Error.Validation("Ticket.EpicNotFound", "The selected epic does not exist.");
        }

        if (epic.TeamId != teamId)
        {
            return Error.Validation("Ticket.EpicWrongTeam", "The epic must belong to the ticket's team.");
        }

        return null;
    }

    // Resolves the display values for an epic change in a single query.
    // A null epic id means "no epic" and is recorded as "None".
    private async Task<(string? Old, string? New)> ResolveEpicValuesAsync(Guid? oldId, Guid? newId, CancellationToken ct)
    {
        var ids = new List<Guid>();
        if (oldId is { } o) ids.Add(o);
        if (newId is { } n) ids.Add(n);

        var titles = ids.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Epics
                .Where(e => ids.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Title, ct);

        string? Resolve(Guid? epicId) =>
            epicId is { } id ? titles.GetValueOrDefault(id) : "None";

        return (Resolve(oldId), Resolve(newId));
    }

    // Resolves the team names for a team change in a single query.
    private async Task<(string? Old, string? New)> ResolveTeamValuesAsync(Guid oldId, Guid newId, CancellationToken ct)
    {
        var names = await _db.Teams
            .Where(t => t.Id == oldId || t.Id == newId)
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return (names.GetValueOrDefault(oldId), names.GetValueOrDefault(newId));
    }
}
