using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Teams;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/teams")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;
    private readonly IValidator<CreateTeamRequest> _createValidator;
    private readonly IValidator<UpdateTeamRequest> _updateValidator;

    public TeamsController(
        ITeamService teams,
        IValidator<CreateTeamRequest> createValidator,
        IValidator<UpdateTeamRequest> updateValidator)
    {
        _teams = teams;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => ApiResults.Success(await _teams.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _teams.GetAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _teams.CreateAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _teams.UpdateAsync(id, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _teams.DeleteAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(new { deleted = true });
    }
}
