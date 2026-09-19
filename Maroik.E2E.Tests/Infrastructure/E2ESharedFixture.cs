using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Infrastructure;

/// <summary>
/// Shared fixture for all E2E test classes in the "E2E" collection.
/// Creates ONE seeded PostgreSQL container (<see cref="E2EPostgresContainer"/>), ONE
/// <see cref="PlaywrightWebApplicationFactory"/> (so Program.cs runs exactly once per test
/// session) and ONE headless Chromium browser instance.
/// Each test creates its own <see cref="IBrowserContext"/> via
/// <see cref="PlaywrightWebApplicationFactory.ServerBaseUrl"/>, ensuring page isolation.
/// </summary>
public sealed class E2ESharedFixture : IAsyncLifetime
{
    private readonly E2EPostgresContainer _database = new();
    private PlaywrightWebApplicationFactory? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>The shared headless Chromium browser instance used by every test.</summary>
    public IBrowser Browser => _browser
        ?? throw new InvalidOperationException("The shared browser has not been initialized.");

    /// <summary>Direct access to the seeded database, for seeding fixtures and asserting persisted rows.</summary>
    public E2EDatabase Database => new(_database.ConnectionString);

    /// <summary>Base URL of the in-process test server hosting the application.</summary>
    public string ServerBaseUrl => _factory!.ServerBaseUrl;

    /// <summary>Starts the database container and in-process server, then launches the shared browser instance.</summary>
    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        _factory = new PlaywrightWebApplicationFactory(_database.ConnectionString);
        _factory.CreateClient();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Channel = "chrome",
            Args = ["--no-sandbox"]
        });
    }

    /// <summary>Tears down the browser, Playwright driver, in-process server, and database container.</summary>
    /// <remarks>
    /// Every step is null-guarded: if <see cref="InitializeAsync"/> throws part-way (e.g. the host
    /// fails to start), xUnit still calls this, and it must surface that original error rather than
    /// mask it with a <see cref="NullReferenceException"/> on a member that was never assigned.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _factory?.Dispose();
        await _database.DisposeAsync();
    }
}
