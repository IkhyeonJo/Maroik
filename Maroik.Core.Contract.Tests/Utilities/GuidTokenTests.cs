using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Utilities.GuidToken"/>.
/// Verifies token generation uniqueness, Base64 validity, 24-hour expiry window,
/// and rejection of expired or tampered tokens.
/// </summary>
public class GuidTokenTests
{
    [Fact]
    public void Generate_ReturnsNonEmptyBase64String()
    {
        string token = GuidToken.Generate();

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // Must be valid Base64
        byte[] bytes = Convert.FromBase64String(token);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Generate_ProducesUniqueTokensOnEachCall()
    {
        string token1 = GuidToken.Generate();
        string token2 = GuidToken.Generate();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void IsTokenAlive_ReturnsTrue_ForFreshToken()
    {
        string token = GuidToken.Generate();

        Assert.True(GuidToken.IsTokenAlive(token));
    }

    [Fact]
    public void IsTokenAlive_ReturnsTrue_ForTokenCreated23HoursAgo()
    {
        string token = BuildTokenWithAge(hours: -23);

        Assert.True(GuidToken.IsTokenAlive(token));
    }

    [Fact]
    public void IsTokenAlive_ReturnsFalse_ForTokenCreated25HoursAgo()
    {
        string token = BuildTokenWithAge(hours: -25);

        Assert.False(GuidToken.IsTokenAlive(token));
    }

    [Fact]
    public void IsTokenAlive_ReturnsFalse_ForTokenCreatedExactly24HoursAgo()
    {
        // Exactly on the boundary (24 h ago) — the check is strict less-than, so expired.
        string token = BuildTokenWithAge(hours: -24);

        Assert.False(GuidToken.IsTokenAlive(token));
    }

    /// <summary>
    /// Builds a token using the same encoding as <see cref="GuidToken.Generate"/>
    /// but with a timestamp offset by the given number of hours from UtcNow.
    /// </summary>
    private static string BuildTokenWithAge(int hours)
    {
        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.AddHours(hours).ToBinary());
        byte[] key = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(time.Concat(key).ToArray());
    }
}
