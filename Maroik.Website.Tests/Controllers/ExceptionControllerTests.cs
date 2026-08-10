using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for <c>ExceptionController</c>. <c>Error</c> is explicitly allow-listed in
/// <see cref="Maroik.Website.Filters.AuthorizationFilter"/> for both anonymous and logged-in GET
/// requests, so it must render regardless of session state. <c>AccessDenied</c> is not in that
/// allow-list, so a direct GET is redirected away by the filter before the view ever renders.
/// </summary>
[Collection("Website Integration")]
public class ExceptionControllerTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of ExceptionControllerTests, sharing the MaroikWebApplicationFactory fixture across the tests in this class.</summary>
    public ExceptionControllerTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // -- Error ----------------------------------------------------------------

    /// <summary>Error anonymous session returns200 and request id.</summary>
    [Fact]
    public async Task Error_AnonymousSession_Returns200AndRequestId()
    {
        var response = await _client.GetAsync("/Exception/Error", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        // No Activity.Current in a plain HTTP request, so ErrorViewModel falls back to
        // HttpContext.TraceIdentifier, which is always populated -> ShowRequestId is true.
        Assert.Contains("Request ID:", html);
    }

    /// <summary>Error logged in user returns200.</summary>
    [Fact]
    public async Task Error_LoggedInUser_Returns200()
    {
        var session = await AuthenticatedSessionHelper.LoginAsync(
            _factory, _client, "exception-error-user@test.com", "UserPassword1!", Maroik.Core.Domain.Account.Role.User,
            TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Exception/Error");
        request.Headers.Add("Cookie", session.CookieHeader);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Error response is not cached.</summary>
    [Fact]
    public async Task Error_ResponseIsNotCached()
    {
        var response = await _client.GetAsync("/Exception/Error", TestContext.Current.CancellationToken);

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    // -- AccessDenied -----------------------------------------------------------

    /// <summary>Access denied anonymous session is redirected by authorization filter.</summary>
    [Fact]
    public async Task AccessDenied_AnonymousSession_IsRedirectedByAuthorizationFilter()
    {
        // AccessDenied is not allow-listed in AuthorizationFilter (only "Exception"/"Error" is),
        // and it has no registered menu Category, so an anonymous GET never reaches the view.
        var response = await _client.GetAsync("/Exception/AccessDenied", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Dashboard/AnonymousIndex", response.Headers.Location!.ToString());
    }

    /// <summary>Access denied logged in user is redirected by authorization filter.</summary>
    [Fact]
    public async Task AccessDenied_LoggedInUser_IsRedirectedByAuthorizationFilter()
    {
        var session = await AuthenticatedSessionHelper.LoginAsync(
            _factory, _client, "exception-accessdenied-user@test.com", "UserPassword1!", Maroik.Core.Domain.Account.Role.User,
            TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Exception/AccessDenied");
        request.Headers.Add("Cookie", session.CookieHeader);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Dashboard/AnonymousIndex", response.Headers.Location!.ToString());
    }
}
