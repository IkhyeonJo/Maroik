using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Infrastructure;

/// <summary>
/// Base class for Playwright E2E tests.
///
/// Lifecycle per test class:
/// <list type="number">
///   <item>The <see cref="PlaywrightWebApplicationFactory"/> starts a real Kestrel server.</item>
///   <item><see cref="Playwright.CreateAsync"/> instantiates the Playwright engine.</item>
///   <item><see cref="IBrowser"/> launches headless Chromium.</item>
///   <item>Each test creates a fresh <see cref="IPage"/> in a new browser context (no shared cookies/session).</item>
///   <item>Everything is torn down in <see cref="DisposeAsync"/>.</item>
/// </list>
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected PlaywrightWebApplicationFactory Factory { get; } = new();
    protected string BaseUrl => Factory.ServerBaseUrl;

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        // Trigger server start by making one HTTP request
        Factory.CreateClient();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        Factory.Dispose();
    }

    /// <summary>Creates a fresh <see cref="IPage"/> in a new isolated browser context.</summary>
    protected async Task<IPage> NewPageAsync()
    {
        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl
        });
        return await context.NewPageAsync();
    }

    /// <summary>Navigates <paramref name="page"/> to <paramref name="path"/> and waits for the DOM to be ready.</summary>
    protected static async Task GotoAsync(IPage page, string path)
    {
        await page.GotoAsync(path, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });
    }
}
