using Maroik.Core.Domain.Account;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>DriveController</c>. Drive is unlaunched (hidden WIP), so it
/// has no real Category rows in production; this test factory seeds a synthetic
/// Admin/User/Anonymous entry for each index (Controller="Drive") so the role-appropriate index
/// still renders under <c>AuthorizationFilter</c>'s menu match.
/// </summary>
[Collection("Website Integration")]
public class DriveControllerTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of DriveControllerTests, sharing the MaroikWebApplicationFactory fixture across the tests in this class.</summary>
    public DriveControllerTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string? role = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (role != null)
        {
            var session = await AuthenticatedSessionHelper.LoginAsync(
                _factory, _client, $"drive-smoke-{role.ToLowerInvariant()}@test.com", "Passw0rd1!", role, TestContext.Current.CancellationToken);
            request.Headers.Add("Cookie", session.CookieHeader);
        }
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Anonymous index get returns200.</summary>
    [Fact]
    public async Task AnonymousIndex_Get_Returns200()
    {
        var response = await GetAsync("/Drive/AnonymousIndex");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Admin index get logged in admin returns200.</summary>
    [Fact]
    public async Task AdminIndex_Get_LoggedInAdmin_Returns200()
    {
        var response = await GetAsync("/Drive/AdminIndex", Role.Admin);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>User index get logged in user returns200.</summary>
    [Fact]
    public async Task UserIndex_Get_LoggedInUser_Returns200()
    {
        var response = await GetAsync("/Drive/UserIndex", Role.User);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
