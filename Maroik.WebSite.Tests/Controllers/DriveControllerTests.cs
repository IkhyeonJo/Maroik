using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>DriveController</c>.
/// All three index views (Admin/User/Anonymous) simply return <c>View()</c>
/// with no session dependency and must respond 200 OK.
/// </summary>
public class DriveControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DriveControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    /// <summary>Verifies that <c>AnonymousIndex</c> get when returns200.</summary>
    [Fact]
    public async Task AnonymousIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/AnonymousIndex", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that <c>AdminIndex</c> get when returns200.</summary>
    [Fact]
    public async Task AdminIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/AdminIndex", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that <c>UserIndex</c> get when returns200.</summary>
    [Fact]
    public async Task UserIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/UserIndex", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
