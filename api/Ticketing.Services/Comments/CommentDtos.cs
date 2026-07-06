namespace Ticketing.Services.Comments;

public record CommentDto(
    Guid Id,
    Guid TicketId,
    Guid AuthorId,
    string? AuthorName,
    string Body,
    DateTime CreatedAt);

public record CreateCommentRequest(string Body);
