using ErrorOr;

namespace Ticketing.Services.Profile;

public interface IProfileService
{
    Task<ErrorOr<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<ProfileDto>> UpdateDisplayNameAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
