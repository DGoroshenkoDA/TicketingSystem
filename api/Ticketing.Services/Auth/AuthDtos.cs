namespace Ticketing.Services.Auth;

public record SignupRequest(string Email, string DisplayName, string Password, string PasswordConfirm);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record UserDto(Guid Id, string Email, string DisplayName);

public record AuthResult(
    string AccessToken,
    DateTime AccessExpiresAt,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    UserDto User);
