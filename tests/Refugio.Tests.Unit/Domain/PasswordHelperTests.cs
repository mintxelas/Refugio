using Refugio.Domain.Helpers;

namespace Refugio.Tests.Domain;

public class PasswordHelperTests
{
    [Fact]
    public void Hash_ProducesValidTwoPart_Format()
    {
        var hash = PasswordHelper.Hash("password123");
        var parts = hash.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.NotEmpty(parts[0]);
        Assert.NotEmpty(parts[1]);
    }

    [Fact]
    public void Hash_ProducesDifferentValues_ForSameInput()
    {
        var h1 = PasswordHelper.Hash("same");
        var h2 = PasswordHelper.Hash("same");
        Assert.NotEqual(h1, h2); // different random salts
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = PasswordHelper.Hash("mySecret");
        Assert.True(PasswordHelper.Verify("mySecret", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = PasswordHelper.Hash("correctPassword");
        Assert.False(PasswordHelper.Verify("wrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash_NoColon()
    {
        Assert.False(PasswordHelper.Verify("password", "notavalidhashstring"));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForEmptyString()
    {
        Assert.False(PasswordHelper.Verify("password", ""));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTooManyColons()
    {
        Assert.False(PasswordHelper.Verify("password", "a:b:c"));
    }

    [Fact]
    public void Verify_ReturnsTrue_WithLongPassword()
    {
        var pw = new string('x', 256);
        var hash = PasswordHelper.Hash(pw);
        Assert.True(PasswordHelper.Verify(pw, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_CaseSensitive()
    {
        var hash = PasswordHelper.Hash("Password");
        Assert.False(PasswordHelper.Verify("password", hash));
    }
}
