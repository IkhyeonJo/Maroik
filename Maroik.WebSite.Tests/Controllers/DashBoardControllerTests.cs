using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>DashBoardController</c>.
/// The anonymous dashboard is the application's default route and must be
/// reachable without authentication.
/// </summary>
[Collection("WebSite Integration")]
public class DashBoardControllerTests
{
    private readonly HttpClient _client;

    public DashBoardControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // -- AnonymousIndex -------------------------------------------------------

    /// <summary>Verifies that <c>AnonymousIndex</c> get when returns200.</summary>
    [Fact]
    public async Task AnonymousIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/DashBoard/AnonymousIndex", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that <c>DefaultRoute</c> get when returns200.</summary>
    [Fact]
    public async Task DefaultRoute_Get_Returns200()
    {
        // The default route pattern is {controller=DashBoard}/{action=AnonymousIndex}/{id?}
        var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
