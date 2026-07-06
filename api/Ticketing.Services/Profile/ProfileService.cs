using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Services.Auth;

namespace Ticketing.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly TicketingDbContext _db;
    private readonly IPasswordHasher _hasher;

    public ProfileService(TicketingDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<ErrorOr<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        return user is null
            ? Error.NotFound("Profile.NotFound", "User not found.")
            : new ProfileDto(user.Id, user.Email, user.DisplayName, user.IsVerified);
    }

    public async Task<ErrorOr<ProfileDto>> UpdateDisplayNameAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Error.NotFound("Profile.NotFound", "User not found.");
        }

        var name = request.DisplayName.Trim();
        if (name.Length == 0)
        {
            return Error.Validation("Profile.DisplayNameRequired", "Display name is required.");
        }

        if (user.DisplayName != name)
        {
            user.DisplayName = name;
            user.ModifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return new ProfileDto(user.Id, user.Email, user.DisplayName, user.IsVerified);
    }

    public async Task<ErrorOr<Success>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Error.NotFound("Profile.NotFound", "User not found.");
        }

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Error.Unauthorized("Profile.WrongPassword", "The current password is incorrect.");
        }

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Error.Validation("Profile.WeakPassword", "New password must be at least 8 characters.");
        }

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success;
    }
}
