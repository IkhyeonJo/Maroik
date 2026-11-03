using Maroik.Core.Contract.Dtos;
using Maroik.Website.Constants;
using Maroik.Website.Extensions;
using Microsoft.AspNetCore.Http;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="HttpContextAccountExtensions.GetLoggedInAccount"/> -- the typed read of the
/// account <c>ViewBagPopulatorFilter</c> publishes on <c>HttpContext.Items</c>.
/// </summary>
public class HttpContextAccountExtensionsTests
{
    /// <summary>Returns exactly the account stored under the logged-in-account key.</summary>
    [Fact]
    public void GetLoggedInAccount_ReturnsTheStoredAccount()
    {
        var account = new AccountResponse { Email = "user@test.com" };
        var context = new DefaultHttpContext { Items = { [HttpContextItemKeys.LoggedInAccount] = account } };

        Assert.Same(account, context.GetLoggedInAccount());
    }

    /// <summary>
    /// With no account resolved for the request (the filter did not run), it throws instead of handing back
    /// null or a made-up anonymous account, so a missing pipeline step fails loudly.
    /// </summary>
    [Fact]
    public void GetLoggedInAccount_Throws_WhenNoAccountWasResolved()
    {
        var context = new DefaultHttpContext();

        var ex = Assert.Throws<InvalidOperationException>(context.GetLoggedInAccount);
        Assert.Contains("ViewBagPopulatorFilter", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>A value of another type under the key is not an account: it throws rather than mis-casting.</summary>
    [Fact]
    public void GetLoggedInAccount_Throws_WhenTheStoredValueIsNotAnAccount()
    {
        var context = new DefaultHttpContext { Items = { [HttpContextItemKeys.LoggedInAccount] = "not an account" } };

        Assert.Throws<InvalidOperationException>(context.GetLoggedInAccount);
    }

    /// <summary>Returns the email of the account stored under the logged-in-account key.</summary>
    [Fact]
    public void GetLoggedInEmail_ReturnsTheStoredAccountsEmail()
    {
        var context = new DefaultHttpContext { Items = { [HttpContextItemKeys.LoggedInAccount] = new AccountResponse { Email = "user@test.com" } } };

        Assert.Equal("user@test.com", context.GetLoggedInEmail());
    }

    /// <summary>
    /// The anonymous placeholder has no email: it throws instead of handing a null email on to a service, so an
    /// action that needs a signed-in account fails loudly when it is reached without one.
    /// </summary>
    [Fact]
    public void GetLoggedInEmail_Throws_WhenTheAccountHasNoEmail()
    {
        var context = new DefaultHttpContext { Items = { [HttpContextItemKeys.LoggedInAccount] = new AccountResponse { Email = null } } };

        var ex = Assert.Throws<InvalidOperationException>(context.GetLoggedInEmail);
        Assert.Contains($"no {nameof(AccountResponse.Email)}", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>With no account resolved for the request it throws, as <c>GetLoggedInAccount</c> does.</summary>
    [Fact]
    public void GetLoggedInEmail_Throws_WhenNoAccountWasResolved()
    {
        var context = new DefaultHttpContext();

        var ex = Assert.Throws<InvalidOperationException>(context.GetLoggedInEmail);
        Assert.Contains("ViewBagPopulatorFilter", ex.Message, StringComparison.Ordinal);
    }
}
