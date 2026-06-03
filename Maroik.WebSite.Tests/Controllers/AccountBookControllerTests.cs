using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>AccountBookController</c>.
/// The Asset, Income, and Expenditure GET actions return <c>View()</c> for non-AJAX requests
/// (no session dependency for the non-AJAX path), so they must respond 200 OK.
/// </summary>
[Collection("WebSite Integration")]
public class AccountBookControllerTests
{
    private readonly HttpClient _client;

    public AccountBookControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // -- Asset ----------------------------------------------------------------

    /// <summary>Verifies that <c>Asset</c> get when non ajax returns 200.</summary>
    [Fact]
    public async Task Asset_Get_NonAjax_Returns200()
    {
        // Non-AJAX GET falls through to return View() without accessing session
        var response = await _client.GetAsync("/AccountBook/Asset", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- Income ---------------------------------------------------------------

    /// <summary>Verifies that <c>Income</c> get when non ajax returns 200.</summary>
    [Fact]
    public async Task Income_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/AccountBook/Income", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- Expenditure ----------------------------------------------------------

    /// <summary>Verifies that <c>Expenditure</c> get when non ajax returns 200.</summary>
    [Fact]
    public async Task Expenditure_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/AccountBook/Expenditure", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
