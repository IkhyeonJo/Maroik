using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Infrastructure;

/// <summary>
/// Base class for Playwright E2E tests.
/// Receives the shared <see cref="E2ESharedFixture"/> from the "E2E" collection,
/// which holds a single <see cref="PlaywrightWebApplicationFactory"/> and browser
/// instance for the entire test session.  Each test creates a fresh
/// <see cref="IPage"/> in a new isolated browser context via <see cref="NewPageAsync"/>.
/// </summary>
public abstract class E2ETestBase
{
    private readonly E2ESharedFixture _fixture;

    protected string BaseUrl => _fixture.ServerBaseUrl;

    /// <summary>Initializes the base class with the shared fixture supplied by the test collection.</summary>
    protected E2ETestBase(E2ESharedFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Creates a fresh <see cref="IPage"/> in a new isolated browser context.</summary>
    protected async Task<IPage> NewPageAsync()
    {
        var context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true
        });
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Navigates <paramref name="page"/> to <paramref name="path"/> and waits for the DOM to be ready.
    /// Retries on transient network errors (e.g. <c>net::ERR_NETWORK_CHANGED</c>) that Chromium's
    /// network-change notifier can spuriously raise in sandboxed/virtualized CI environments.
    /// </summary>
    protected static async Task GotoAsync(IPage page, string path)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await page.GotoAsync(path, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30_000
                });
                return;
            }
            catch (PlaywrightException ex) when (attempt < maxAttempts && ex.Message.Contains("net::ERR_NETWORK_CHANGED"))
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
