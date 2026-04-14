using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>NoticeController</c>.
/// The FixedIncome and FixedExpenditure GET actions return <c>View()</c> for non-AJAX requests
/// (no session dependency on the non-AJAX path), so they must respond 200 OK.
/// </summary>
public class NoticeControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NoticeControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // ── FixedIncome ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FixedIncome_Get_NonAjax_Returns200()
    {
        // Non-AJAX GET falls through to return View() without accessing session
        var response = await _client.GetAsync("/Notice/FixedIncome", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── FixedExpenditure ─────────────────────────────────────────────────────

    [Fact]
    public async Task FixedExpenditure_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/Notice/FixedExpenditure", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
