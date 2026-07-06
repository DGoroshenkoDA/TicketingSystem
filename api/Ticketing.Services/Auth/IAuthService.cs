using ErrorOr;

namespace Ticketing.Services.Auth;

public interface IAuthService
{
    Task<ErrorOr<UserDto>> SignupAsync(SignupRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthResult>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<ErrorOr<Success>> ResendVerificationAsync(string email, CancellationToken ct = default);
}
