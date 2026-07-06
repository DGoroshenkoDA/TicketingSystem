using System.Linq.Expressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Data.Entities;

namespace Ticketing.Services.Comments;

public class CommentService : ICommentService
{
    private readonly TicketingDbContext _db;

    public CommentService(TicketingDbContext db) => _db = db;

    private static readonly Expression<Func<Comment, CommentDto>> Projection = c => new CommentDto(
        c.Id,
        c.TicketId,
        c.AuthorId,
        c.Author != null ? c.Author.DisplayName : null,
        c.Body,
        c.CreatedAt);

    public async Task<ErrorOr<List<CommentDto>>> ListAsync(Guid ticketId, CancellationToken ct = default)
    {
        if (!await _db.Tickets.AnyAsync(t => t.Id == ticketId, ct))
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        return await _db.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .Select(Projection)
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<CommentDto>> AddAsync(Guid ticketId, CreateCommentRequest request, Guid authorId, CancellationToken ct = default)
    {
        if (!await _db.Tickets.AnyAsync(t => t.Id == ticketId, ct))
        {
            return Error.NotFound("Ticket.NotFound", "Ticket not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Error.Validation("Comment.BodyRequired", "Comment body is required.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Adding a comment intentionally does NOT touch the ticket's modified_at,
        // so it does not change the ticket's board ordering.
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return await _db.Comments.Where(c => c.Id == comment.Id).Select(Projection).FirstAsync(ct);
    }
}
