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

    // -- FreeForum list -------------------------------------------------------

    /// <summary>Verifies that <c>FreeForum</c> default list when returns200.</summary>
    [Fact]
    public async Task FreeForum_DefaultList_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that <c>FreeForum</c> list with title search when returns200.</summary>
    [Fact]
    public async Task FreeForum_ListWithTitleSearch_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=list&searchType=Title&searchText=hello", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that <c>FreeForum</c> list with writer search when returns200.</summary>
    [Fact]
    public async Task FreeForum_ListWithWriterSearch_Returns200()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=list&searchType=Writer&searchText=alice", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- FreeForum write (no session) -----------------------------------------

    /// <summary>Verifies that <c>FreeForum</c> write mode without session when redirects to login.</summary>
    [Fact]
    public async Task FreeForum_WriteModeWithoutSession_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=write", TestContext.Current.CancellationToken);

        // Without a session, the controller redirects to Account/Login
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Login", response.Headers.Location?.ToString() ?? "");
    }

    // -- FreeForum detail (nonexistent board) ---------------------------------

    /// <summary>Verifies that <c>FreeForum</c> detail mode with missing board when redirects to list.</summary>
    [Fact]
    public async Task FreeForum_DetailModeWithMissingBoard_RedirectsToList()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=detail&boardId=99999", TestContext.Current.CancellationToken);

        // Board does not exist → controller redirects back to the list
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>Verifies that <c>FreeForum</c> detail mode with null board id when redirects to list.</summary>
    [Fact]
    public async Task FreeForum_DetailModeWithNullBoardId_RedirectsToList()
    {
        var response = await _client.GetAsync("/Forum/FreeForum?method=detail", TestContext.Current.CancellationToken);

        // boardId is null → RedirectToAction("FreeForum", "Forum")
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }
}
