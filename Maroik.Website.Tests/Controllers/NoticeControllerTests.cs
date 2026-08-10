using Maroik.Core.Contract.Misc.Constants;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>NoticeController</c>.
/// FixedIncome/FixedExpenditure are User-role-gated in the real production menu (Category
/// Controller="Notice", Role=User — loaded from the real Init.sql this test factory now seeds
/// from), so an anonymous GET is correctly redirected rather than rendering; these tests
/// authenticate as a User first.
/// </summary>
[Collection("Website Integration")]
public class NoticeControllerTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NoticeControllerTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private async Task<HttpResponseMessage> GetAsUserAsync(string url)
    {
        var session = await AuthenticatedSessionHelper.LoginAsync(
            _factory, _client, "notice-smoke-user@test.com", "UserPassword1!", Role.User, TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", session.CookieHeader);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // -- FixedIncome ------------------------------------------------------------

    [Fact]
    public async Task FixedIncome_Get_LoggedInUser_Returns200()
    {
        var response = await GetAsUserAsync("/Notice/FixedIncome");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FixedIncome_Get_Anonymous_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Notice/FixedIncome", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // -- FixedExpenditure ---------------------------------------------------------

    [Fact]
    public async Task FixedExpenditure_Get_LoggedInUser_Returns200()
    {
        var response = await GetAsUserAsync("/Notice/FixedExpenditure");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
