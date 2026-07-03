using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>NoticeController</c>.
/// The FixedIncome and FixedExpenditure GET actions return <c>View()</c> for non-AJAX requests
/// (no session dependency on the non-AJAX path), so they must respond 200 OK.
/// </summary>
[Collection("Website Integration")]
public class NoticeControllerTests
{
    private readonly HttpClient _client;

    public NoticeControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // -- FixedIncome ----------------------------------------------------------

    /// <summary>Verifies that <c>FixedIncome</c> get when non ajax returns 200.</summary>
    [Fact]
    public async Task FixedIncome_Get_NonAjax_Returns200()
    {
        // Non-AJAX GET falls through to return View() without accessing session
        var response = await _client.GetAsync("/Notice/FixedIncome", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- FixedExpenditure -----------------------------------------------------

    /// <summary>Verifies that <c>FixedExpenditure</c> get when non ajax returns 200.</summary>
    [Fact]
    public async Task FixedExpenditure_Get_NonAjax_Returns200()
    {
        var response = await _client.GetAsync("/Notice/FixedExpenditure", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
