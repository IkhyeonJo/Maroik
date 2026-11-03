using Maroik.Core.Contract.Dtos;
using Maroik.Website.Extensions;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="AccountResponseExtensions"/> — the checked reads of the request's account fields that the
/// database stores as NOT NULL (and the anonymous placeholder fills, except its email).
/// </summary>
public class AccountResponseExtensionsTests
{
    private static AccountResponse Full() => new()
    {
        Email = "user@test.com",
        Nickname = "user",
        TimeZoneIanaId = "Asia/Seoul",
        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg"
    };

    /// <summary>Each read returns the field's value.</summary>
    [Fact]
    public void TheReads_ReturnTheFieldValues()
    {
        AccountResponse account = Full();

        Assert.Equal("user@test.com", account.RequireEmail());
        Assert.Equal("user", account.RequireNickname());
        Assert.Equal("Asia/Seoul", account.RequireTimeZoneIanaId());
        Assert.Equal("/upload/Management/Profile/default-avatar.jpg", account.RequireAvatarImagePath());
    }

    /// <summary>
    /// A missing value throws, naming the field: an action that needs a signed-in account fails loudly instead of handing a
    /// null (or a made-up fallback such as "UTC") on to a service.
    /// </summary>
    [Theory]
    [InlineData(nameof(AccountResponse.Email))]
    [InlineData(nameof(AccountResponse.Nickname))]
    [InlineData(nameof(AccountResponse.TimeZoneIanaId))]
    [InlineData(nameof(AccountResponse.AvatarImagePath))]
    public void AMissingValue_Throws_NamingTheField(string field)
    {
        AccountResponse account = Full();
        Func<string> read = field switch
        {
            nameof(AccountResponse.Email) => () => { account.Email = null; return account.RequireEmail(); },
            nameof(AccountResponse.Nickname) => () => { account.Nickname = null; return account.RequireNickname(); },
            nameof(AccountResponse.TimeZoneIanaId) => () => { account.TimeZoneIanaId = null; return account.RequireTimeZoneIanaId(); },
            _ => () => { account.AvatarImagePath = null; return account.RequireAvatarImagePath(); },
        };

        var ex = Assert.Throws<InvalidOperationException>(read);
        Assert.Contains(field, ex.Message, StringComparison.Ordinal);
    }
}
