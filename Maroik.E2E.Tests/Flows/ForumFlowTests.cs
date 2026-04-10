using Maroik.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Flows;

/// <summary>
/// End-to-end tests for the FreeForum user flow.
/// The forum list is accessible without authentication; write mode requires a login.
/// </summary>
public class ForumFlowTests : E2ETestBase
{
    // ── Forum list ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FreeForumList_Loads_WithNonEmptyBody()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Forum/FreeForum");

        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    [Fact]
    public async Task FreeForumList_HasFreeBoardHeading()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Forum/FreeForum");

        // The forum list view renders an <h3 class="card-title"> with the localised "Free Forum" text
        var heading = page.Locator("h3.card-title");
        await Assertions.Expect(heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task FreeForumList_ContainsSearchForm()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Forum/FreeForum");

        // The forum list includes a search form or at least one form element
        int formCount = await page.Locator("form").CountAsync();
        Assert.True(formCount > 0, "Forum list should include at least one form (search / write).");
    }

    // ── Write mode: unauthenticated ──────────────────────────────────────────

    [Fact]
    public async Task FreeForumWriteMode_WithoutSession_RedirectsToLogin()
    {
        var page = await NewPageAsync();

        // Navigate to write mode — without a session the controller redirects to Login
        await GotoAsync(page, "/Forum/FreeForum?method=write");

        // After following the redirect, the page URL should contain "Login"
        string finalUrl = page.Url;
        Assert.Contains("Login", finalUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FreeForumWriteMode_WithoutSession_ShowsLoginForm()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Forum/FreeForum?method=write");

        // After redirect to Login, the login form must be present
        await page.WaitForSelectorAsync("#loginForm", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await Assertions.Expect(page.Locator("#loginForm")).ToBeVisibleAsync();
    }

    // ── Detail mode: missing board ────────────────────────────────────────────

    [Fact]
    public async Task FreeForumDetailMode_WithMissingBoardId_RedirectsToList()
    {
        var page = await NewPageAsync();

        // boardId 99999 does not exist → controller redirects back to the list
        await GotoAsync(page, "/Forum/FreeForum?method=detail&boardId=99999");

        // After following the redirect, the h3 "Free Forum" heading must be present on the list
        var heading = page.Locator("h3.card-title");
        await Assertions.Expect(heading).ToBeVisibleAsync();
    }

    // ── Title search ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FreeForumList_WithTitleSearch_RendersPage()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Forum/FreeForum?method=list&searchType=Title&searchText=hello");

        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }
}
