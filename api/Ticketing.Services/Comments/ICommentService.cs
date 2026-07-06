using ErrorOr;

namespace Ticketing.Services.Comments;

public interface ICommentService
{
    Task<ErrorOr<List<CommentDto>>> ListAsync(Guid ticketId, CancellationToken ct = default);
    Task<ErrorOr<CommentDto>> AddAsync(Guid ticketId, CreateCommentRequest request, Guid authorId, CancellationToken ct = default);
}
