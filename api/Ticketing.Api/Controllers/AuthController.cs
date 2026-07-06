using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Api.Common;
using Ticketing.Services.Auth;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<SignupRequest> _signupValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly IValidator<ResendVerificationRequest> _resendValidator;

    public AuthController(
        IAuthService auth,
        IValidator<SignupRequest> signupValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshRequest> refreshValidator,
        IValidator<LogoutRequest> logoutValidator,
        IValidator<ResendVerificationRequest> resendValidator)
    {
        _auth = auth;
        _signupValidator = signupValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
        _resendValidator = resendValidator;
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request, CancellationToken ct)
    {
        var validation = await _signupValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(FirstError(validation));
        }

        var result = await _auth.SignupAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(FirstError(validation));
        }

        var result = await _auth.LoginAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(FirstError(validation));
        }

        var result = await _auth.RefreshAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(result.Value);
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string? token, CancellationToken ct)
    {
        var result = await _auth.VerifyEmailAsync(token ?? string.Empty, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(new { verified = true });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        var validation = await _resendValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(FirstError(validation));
        }

        var result = await _auth.ResendVerificationAsync(request.Email, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(new { sent = true });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var validation = await _logoutValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationFailure(FirstError(validation));
        }

        var result = await _auth.LogoutAsync(request, ct);
        return result.IsError
            ? ApiResults.Failure(result.FirstError)
            : ApiResults.Success(new { loggedOut = true });
    }

    private static string FirstError(FluentValidation.Results.ValidationResult validation)
        => validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
