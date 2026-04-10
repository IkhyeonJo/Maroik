using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>ManagementController</c>.
/// The Account and Menu GET actions return <c>View()</c> for non-AJAX requests
/// (no session dependency on the non-AJAX path), so they must respond 200 OK.
/// </summary>
public class ManagementControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ManagementControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // ── Account ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Account_Get_NonAjax_Returns200()
    {
        // Non-AJAX GET falls through to return View() without accessing session
        var response = await _client.GetAsync("/Management/Account");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Menu ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Menu_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/Management/Menu");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
