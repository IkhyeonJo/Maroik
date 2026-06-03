using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Infrastructure;

/// <summary>
/// Shared fixture for all E2E test classes in the "E2E" collection.
/// Creates ONE <see cref="PlaywrightWebApplicationFactory"/> (so Program.cs runs
/// exactly once per test session) and ONE headless Chromium browser instance.
/// Each test creates its own <see cref="IBrowserContext"/> via
/// <see cref="PlaywrightWebApplicationFactory.ServerBaseUrl"/>, ensuring page isolation.
/// </summary>
public sealed class E2ESharedFixture : IAsyncLifetime
{
    private readonly PlaywrightWebApplicationFactory _factory = new();
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string ServerBaseUrl => _factory.ServerBaseUrl;

    public async ValueTask InitializeAsync()
    {
        _factory.CreateClient();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Channel = "chrome"
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright?.Dispose();
        _factory.Dispose();
    }
}
