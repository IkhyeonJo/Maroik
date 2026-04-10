using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>ForumController</c>.
/// The free-forum list view uses <c>GetAccount() ?? anonymous</c> so it works
/// without a session and must respond 200 OK.
/// </summary>
public class ForumControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ForumControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // ── FreeForum list ───────────────────────────────────────────────────────

    [Fact]
    public async Task FreeForum_DefaultList_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FreeForum_ListWithTitleSearch_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=list&searchType=Title&searchText=hello");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FreeForum_ListWithWriterSearch_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=list&searchType=Writer&searchText=alice");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── FreeForum write (no session) ─────────────────────────────────────────

    [Fact]
    public async Task FreeForum_WriteModeWithoutSession_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=write");

        // Without a session, the controller redirects to Account/Login
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Login", response.Headers.Location?.ToString() ?? "");
    }

    // ── FreeForum detail (nonexistent board) ─────────────────────────────────

    [Fact]
    public async Task FreeForum_DetailModeWithMissingBoard_RedirectsToList()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=detail&boardId=99999");

        // Board does not exist → controller redirects back to the list
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task FreeForum_DetailModeWithNullBoardId_RedirectsToList()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=detail");

        // boardId is null → RedirectToAction("FreeForum", "Forum")
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }
}
