using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>Tests of the test host itself (<see cref="MaroikWebApplicationFactory"/>).</summary>
[Collection("Website Integration")]
public class MaroikWebApplicationFactoryTests(MaroikWebApplicationFactory factory)
{
    /// <summary>
    /// The test host does not watch its configuration files. A watched file holds an inotify instance for the
    /// host's lifetime and test hosts are never stopped mid-run, so every derived host (<c>WithWebHostBuilder</c>)
    /// would take another one until the per-user limit (128 by default) is reached — after which every later host
    /// in the run fails to start ("The configured user limit (128) on the number of inotify instances has been
    /// reached").
    /// </summary>
    [Fact]
    public void TheTestHost_DoesNotWatchItsConfigurationFiles()
    {
        var configuration = (IConfigurationRoot)factory.Services.GetRequiredService<IConfiguration>();

        List<FileConfigurationProvider> fileProviders = [.. configuration.Providers.OfType<FileConfigurationProvider>()];

        Assert.NotEmpty(fileProviders);
        Assert.All(fileProviders, provider => Assert.False(provider.Source.ReloadOnChange, provider.Source.Path));
    }

    /// <summary>
    /// The test host does not connect to Valkey. There is none in the test run, and <c>Program.cs</c> reads the connection
    /// string before <c>ConfigureAppConfiguration</c> overrides apply, so the Development value (<c>localhost:6379</c>) would
    /// win: every host — the base one and each <c>WithWebHostBuilder</c> host — then waited out StackExchange.Redis'
    /// 5-second backlog timeout while the Data Protection key ring was read at start-up (about 10 s per host, the larger
    /// part of this project's run time).
    /// </summary>
    [Fact]
    public void TheTestHost_DoesNotConnectToValkey()
    {
        Assert.Null(factory.Services.GetService<IConnectionMultiplexer>());
    }

    /// <summary>
    /// <c>Program.cs</c> builds the session and cookie set-up from the factory's settings, not from appsettings.Development.json.
    /// It reads them before <c>ConfigureAppConfiguration</c> overrides apply, so the factory hands them over as host settings;
    /// otherwise the host ran on a mix — Development's domain (<c>localhost</c>), 10-minute sessions and 3 login attempts at
    /// start-up, the factory's values (<c>www.localhost</c>, 60, 5) everywhere else — and needed a cookie-domain patch.
    /// </summary>
    [Fact]
    public void TheTestHost_StartsProgramWithTheFactorysServerSettings()
    {
        SessionOptions session = factory.Services.GetRequiredService<IOptions<SessionOptions>>().Value;
        AntiforgeryOptions antiforgery = factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(60), session.IdleTimeout);
        Assert.Equal("www.localhost", session.Cookie.Domain);
        Assert.Equal("www.localhost", antiforgery.Cookie.Domain);
    }
}
