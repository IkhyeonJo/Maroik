using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.Management;

namespace Maroik.Website.Tests.Extensions;

/// <summary>Unit tests for <see cref="ManagementSearchExtensions"/>.</summary>
public class ManagementSearchExtensionsTests
{
    private static List<AccountOutputViewModel> SampleAccounts() =>
    [
        new()
        {
            Email = "admin@test.com", Nickname = "Admin", Role = "Admin", TimeZoneIanaId = "Asia/Seoul",
            Locked = false, LoginAttempt = 0, EmailConfirmed = false, Message = "welcome"
        },
        new()
        {
            Email = "user@test.com", Nickname = "RegularUser", Role = "User", TimeZoneIanaId = "UTC",
            Locked = true, LoginAttempt = 3, EmailConfirmed = false, Message = ""
        }
    ];

    /// <summary>Empty search returns all.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptySearch_ReturnsAll(string? search)
    {
        var result = SampleAccounts().ApplySearch(search);
        Assert.Equal(2, result.Count());
    }

    /// <summary>Matches email.</summary>
    [Fact]
    public void MatchesEmail()
    {
        var result = SampleAccounts().ApplySearch("admin@test.com").ToList();
        Assert.Single(result);
        Assert.Equal("Admin", result[0].Nickname);
    }

    /// <summary>Matches nickname.</summary>
    [Fact]
    public void MatchesNickname()
    {
        var result = SampleAccounts().ApplySearch("RegularUser").ToList();
        Assert.Single(result);
        Assert.Equal("user@test.com", result[0].Email);
    }

    /// <summary>Matches role.</summary>
    [Fact]
    public void MatchesRole()
    {
        var result = SampleAccounts().ApplySearch("User").ToList();
        // "User" is a substring of both the Role "User" and the Nickname "RegularUser"
        Assert.Single(result);
        Assert.Equal("user@test.com", result[0].Email);
    }

    /// <summary>Matches time zone iana id.</summary>
    [Fact]
    public void MatchesTimeZoneIanaId()
    {
        var result = SampleAccounts().ApplySearch("Asia/Seoul").ToList();
        Assert.Single(result);
        Assert.Equal("admin@test.com", result[0].Email);
    }

    /// <summary>Matches locked boolean as string.</summary>
    [Fact]
    public void MatchesLockedBooleanAsString()
    {
        var result = SampleAccounts().ApplySearch("True").ToList();
        Assert.Single(result);
        Assert.Equal("user@test.com", result[0].Email);
    }

    /// <summary>Matches login attempt number as string.</summary>
    [Fact]
    public void MatchesLoginAttemptNumberAsString()
    {
        var result = SampleAccounts().ApplySearch("3").ToList();
        Assert.Single(result);
        Assert.Equal("user@test.com", result[0].Email);
    }

    /// <summary>No match returns empty.</summary>
    [Fact]
    public void NoMatch_ReturnsEmpty()
    {
        var result = SampleAccounts().ApplySearch("nonexistent-xyz");
        Assert.Empty(result);
    }
}
