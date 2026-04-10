using Maroik.Core.PostgreSQL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maroik.E2E.Tests.Infrastructure;

/// <summary>
/// Extends <see cref="WebApplicationFactory{TEntryPoint}"/> to start a real Kestrel
/// server on a random available port so Playwright (which runs in a separate browser
/// process) can reach it via a real TCP connection.
///
/// Design note: the standard <c>TestServer</c> used by <see cref="WebApplicationFactory{T}"/>
/// is an in-process, socket-less transport that Playwright cannot access.  By overriding
/// <see cref="CreateHost"/> we spin up a <em>second</em> host that binds a real port while
/// still returning the original test-host so the base-class HttpClient helpers work normally.
/// </summary>
public sealed class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _realHost;

    /// <summary>Base URL of the real Kestrel server (e.g. <c>http://localhost:57432</c>).</summary>
    public string ServerBaseUrl { get; private set; } = string.Empty;

    // ── WebApplicationFactory overrides ──────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ── Override configuration ───────────────────────────────────────────
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ServerSetting:DockerCertPath"] = "",
                ["ServerSetting:DockerKeyPath"] = "",
                ["ServerSetting:DomainName"] = "http://localhost",
                ["ServerSetting:SessionExpireMinutes"] = "60",
                ["ServerSetting:MaxLoginAttempt"] = "5",
                ["ServerSetting:NoticeMaturityDateDay"] = "7",
                ["ServerSetting:RsaPrivateKey"] = "",
                ["ServerSetting:RsaPublicKey"] = "",
                ["ServerSetting:RsaAlgorithm"] = "Rsa2",
                ["ServerSetting:FileStorageBaseUrl"] = "http://localhost:5001",
                ["ServerSetting:SmtpOptions:smtpUserName"] = "test",
                ["ServerSetting:SmtpOptions:smtpPassword"] = "test",
                ["ServerSetting:SmtpOptions:smtpHost"] = "localhost",
                ["ServerSetting:SmtpOptions:smtpPort"] = "25",
                ["ServerSetting:SmtpOptions:smtpSsl"] = "false",
                ["ServerSetting:SmtpOptions:fromEmail"] = "test@localhost",
                ["ServerSetting:SmtpOptions:fromFullName"] = "Test",
                ["ServerSetting:SmtpOptions:IsDefault"] = "true",
                ["Clamav:Host"] = "localhost",
                ["Clamav:Port"] = "3310",
                ["FileStorage:BaseUrl"] = "http://localhost:5001",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            }!);
        });

        // ── Replace PostgreSQL with InMemory ────────────────────────────────
        builder.ConfigureServices(services =>
        {
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"E2EIntegration_{Guid.NewGuid()}"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the standard in-process test host (used by base-class HttpClient helpers)
        var testHost = builder.Build();

        // Build a second host with a real Kestrel listener on any available port
        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel(o => o.ListenAnyIP(0)));

        _realHost = builder.Build();
        _realHost.Start();

        // Discover the actual port that Kestrel chose
        var server = _realHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerBaseUrl = addresses!.Addresses.Last();

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _realHost?.Dispose();
        base.Dispose(disposing);
    }
}
