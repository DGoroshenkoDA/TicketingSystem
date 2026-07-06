using Ticketing.Services.Auth;
using Xunit;

namespace Ticketing.Tests;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new Argon2idPasswordHasher();

    [Fact]
    public void Hash_Is_Not_Plaintext()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain("correct horse battery staple", hash);
    }

    [Fact]
    public void Verify_Returns_True_For_Correct_Password()
    {
        var hash = _hasher.Hash("s3cretPassword");

        Assert.True(_hasher.Verify("s3cretPassword", hash));
    }

    [Fact]
    public void Verify_Returns_False_For_Wrong_Password()
    {
        var hash = _hasher.Hash("s3cretPassword");

        Assert.False(_hasher.Verify("wrongPassword", hash));
    }

    [Fact]
    public void Two_Hashes_Of_Same_Password_Differ_By_Salt()
    {
        var a = _hasher.Hash("samePassword123");
        var b = _hasher.Hash("samePassword123");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify("samePassword123", a));
        Assert.True(_hasher.Verify("samePassword123", b));
    }
}
