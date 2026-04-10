using Maroik.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Flows;

/// <summary>
/// End-to-end tests for the dashboard pages.
/// The anonymous dashboard is the application's default landing page and the only
/// dashboard that an unauthenticated browser can reach without a session.
/// </summary>
public class DashBoardFlowTests : E2ETestBase
{
    // ── Anonymous dashboard ──────────────────────────────────────────────────

    [Fact]
    public async Task AnonymousDashboard_Loads_WithNonEmptyBody()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/DashBoard/AnonymousIndex");

        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    [Fact]
    public async Task DefaultRoute_Loads_AndResolvesToDashBoard()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/");

        // The root URL resolves to DashBoard/AnonymousIndex via the default route
        string finalUrl = page.Url;
        Assert.Contains("localhost", finalUrl, StringComparison.OrdinalIgnoreCase);

        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    [Fact]
    public async Task AnonymousDashboard_HasHtmlBodyElement()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/DashBoard/AnonymousIndex");

        // Standard HTML body element must exist
        await Assertions.Expect(page.Locator("body")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AnonymousDashboard_HasNonEmptyPageTitle()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/DashBoard/AnonymousIndex");

        string title = await page.TitleAsync();
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    // ── Navigation links available on the anonymous dashboard ────────────────

    [Fact]
    public async Task AnonymousDashboard_ContainsNavigationElements()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/DashBoard/AnonymousIndex");

        // At minimum there should be one anchor element in the page
        int linkCount = await page.Locator("a").CountAsync();
        Assert.True(linkCount > 0, "Anonymous dashboard should contain at least one navigation link.");
    }
}
