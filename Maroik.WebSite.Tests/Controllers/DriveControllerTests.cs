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

    [Fact]
    public async Task AnonymousIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/AnonymousIndex");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/AdminIndex");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Drive/UserIndex");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
