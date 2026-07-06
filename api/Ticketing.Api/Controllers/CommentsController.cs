using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Comments;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/tickets/{ticketId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;
    private readonly IValidator<CreateCommentRequest> _validator;

    public CommentsController(ICommentService comments, IValidator<CreateCommentRequest> validator)
    {
        _comments = comments;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid ticketId, CancellationToken ct)
    {
        var result = await _comments.ListAsync(ticketId, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid ticketId, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        if (CurrentUserId() is not { } userId)
        {
            return ApiResults.ValidationFailure("Could not determine the authenticated user.");
        }

        var result = await _comments.AddAsync(ticketId, request, userId, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value, StatusCodes.Status201Created);
    }

    private Guid? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
