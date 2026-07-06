using ErrorOr;
using Ticketing.Services.Profile;
using Xunit;

namespace Ticketing.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task Update_DisplayName_Changes_Name()
    {
        using var db = TestSupport.NewDb();
        var userId = TestSupport.AddUserWithPassword(db, "oldPassword1");
        var profile = TestSupport.NewProfileService(db);

        var result = await profile.UpdateDisplayNameAsync(userId, new UpdateProfileRequest("  New Name  "));

        Assert.False(result.IsError);
        Assert.Equal("New Name", result.Value.DisplayName);
    }

    [Fact]
    public async Task Update_Empty_DisplayName_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var userId = TestSupport.AddUserWithPassword(db, "oldPassword1");
        var profile = TestSupport.NewProfileService(db);

        var result = await profile.UpdateDisplayNameAsync(userId, new UpdateProfileRequest("   "));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task ChangePassword_With_Wrong_Current_Returns_Unauthorized()
    {
        using var db = TestSupport.NewDb();
        var userId = TestSupport.AddUserWithPassword(db, "oldPassword1");
        var profile = TestSupport.NewProfileService(db);

        var result = await profile.ChangePasswordAsync(userId, new ChangePasswordRequest("wrong", "newPassword1"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }

    [Fact]
    public async Task ChangePassword_Too_Short_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var userId = TestSupport.AddUserWithPassword(db, "oldPassword1");
        var profile = TestSupport.NewProfileService(db);

        var result = await profile.ChangePasswordAsync(userId, new ChangePasswordRequest("oldPassword1", "short"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task ChangePassword_Success_Updates_Hash()
    {
        using var db = TestSupport.NewDb();
        var userId = TestSupport.AddUserWithPassword(db, "oldPassword1");
        var profile = TestSupport.NewProfileService(db);

        var result = await profile.ChangePasswordAsync(userId, new ChangePasswordRequest("oldPassword1", "newPassword1"));

        Assert.False(result.IsError);
        var hasher = TestSupport.NewHasher();
        var user = await db.Users.FindAsync(userId);
        Assert.True(hasher.Verify("newPassword1", user!.PasswordHash));
        Assert.False(hasher.Verify("oldPassword1", user.PasswordHash));
    }
}
