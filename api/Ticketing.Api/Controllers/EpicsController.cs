using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Epics;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/epics")]
public class EpicsController : ControllerBase
{
    private readonly IEpicService _epics;
    private readonly IValidator<CreateEpicRequest> _createValidator;
    private readonly IValidator<UpdateEpicRequest> _updateValidator;

    public EpicsController(
        IEpicService epics,
        IValidator<CreateEpicRequest> createValidator,
        IValidator<UpdateEpicRequest> updateValidator)
    {
        _epics = epics;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? teamId, CancellationToken ct)
    {
        if (teamId is null || teamId == Guid.Empty)
        {
            return ApiResults.ValidationFailure("teamId query parameter is required.");
        }

        return ApiResults.Success(await _epics.ListAsync(teamId.Value, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _epics.GetAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpicRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _epics.CreateAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEpicRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        var result = await _epics.UpdateAsync(id, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _epics.DeleteAsync(id, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(new { deleted = true });
    }
}
