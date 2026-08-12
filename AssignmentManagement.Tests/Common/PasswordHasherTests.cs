using AssignmentManagement.Infrastructure.Authentication;
using Xunit;

namespace AssignmentManagement.Tests.Common;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_Then_Verify_Succeeds()
    {
        var hash = _hasher.Hash("Secret@123");
        Assert.True(_hasher.Verify("Secret@123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var hash = _hasher.Hash("Secret@123");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_InvalidHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("anything", "not-a-bcrypt-hash"));
    }
}
