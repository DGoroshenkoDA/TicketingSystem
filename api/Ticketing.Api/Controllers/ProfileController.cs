using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Profile;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly IValidator<ChangePasswordRequest> _passwordValidator;

    public ProfileController(
        IProfileService profile,
        IValidator<UpdateProfileRequest> updateValidator,
        IValidator<ChangePasswordRequest> passwordValidator)
    {
        _profile = profile;
        _updateValidator = updateValidator;
        _passwordValidator = passwordValidator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId)
        {
            return ApiResults.ValidationFailure("Could not determine the authenticated user.");
        }

        var result = await _profile.GetAsync(userId, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        if (CurrentUserId() is not { } userId)
        {
            return ApiResults.ValidationFailure("Could not determine the authenticated user.");
        }

        var result = await _profile.UpdateDisplayNameAsync(userId, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(result.Value);
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var validation = await _passwordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(validation.Errors[0].ErrorMessage);
        }

        if (CurrentUserId() is not { } userId)
        {
            return ApiResults.ValidationFailure("Could not determine the authenticated user.");
        }

        var result = await _profile.ChangePasswordAsync(userId, request, ct);
        return result.IsError ? ApiResults.Failure(result.FirstError) : ApiResults.Success(new { changed = true });
    }

    private Guid? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
