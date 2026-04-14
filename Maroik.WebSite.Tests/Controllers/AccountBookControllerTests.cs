using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>AccountBookController</c>.
/// The Asset, Income, and Expenditure GET actions return <c>View()</c> for non-AJAX requests
/// (no session dependency for the non-AJAX path), so they must respond 200 OK.
/// </summary>
public class AccountBookControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountBookControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // ── Asset ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Asset_Get_NonAjax_Returns200()
    {
        // Non-AJAX GET falls through to return View() without accessing session
        var response = await _client.GetAsync("/AccountBook/Asset", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Income ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Income_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/AccountBook/Income", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Expenditure ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Expenditure_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/AccountBook/Expenditure", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
