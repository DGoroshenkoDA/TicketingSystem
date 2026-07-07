using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Tickets;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly IValidator<CreateTicketRequest> _createValidator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<UpdateTicketStateRequest> _stateValidator;

    public TicketsController(
        ITicketService tickets,
        IValidator<CreateTicketRequest> createValidator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<UpdateTicketStateRequest> stateValidator)
    {
        _tickets = tickets;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _stateValidator = stateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? teamId,
        [FromQuery] string? type,
        [FromQuery] Guid? epicId,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        if (teamId is null || teamId == Guid.Empty)
        {
            return ApiResults.ValidationFailure("teamId query parameter is required.");
        }

        var query = new TicketQuery(teamId.Value, type, epicId, search);
        var result = await _tickets.ListAsync(query, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _tickets.GetAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        if (CurrentUserId() is not { } userId)
        {
            return ApiResults.ValidationFailure("Could not determine the authenticated user.");
        }

        var result = await _tickets.CreateAsync(request, userId, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _tickets.UpdateAsync(id, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPatch("{id:guid}/state")]
    public async Task<IActionResult> UpdateState(Guid id, [FromBody] UpdateTicketStateRequest request, CancellationToken ct)
    {
        var validation = await _stateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _tickets.UpdateStateAsync(id, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _tickets.DeleteAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(new { deleted = true });
    }

    private Guid? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
