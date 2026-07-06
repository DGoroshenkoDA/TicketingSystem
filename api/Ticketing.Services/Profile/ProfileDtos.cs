namespace Ticketing.Services.Profile;

public record ProfileDto(Guid Id, string Email, string DisplayName, bool IsVerified);

public record UpdateProfileRequest(string DisplayName);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
