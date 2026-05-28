using Maroik.Core.Contract.Utilities;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
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

    // -- WebApplicationFactory overrides --------------------------------------

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // -- Override configuration -------------------------------------------
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ServerSetting:DockerCertPath"] = "",
                ["ServerSetting:DockerKeyPath"] = "",
                ["ServerSetting:DomainName"] = "https://www.localhost",
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

        // -- Replace PostgreSQL with InMemory --------------------------------
        builder.ConfigureServices(services =>
        {
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            // Provide an explicit internal service provider so that the double-build
            // pattern (builder.Build() called twice) does not trigger EF Core's
            // "multiple providers registered" error from the shared provider cache.
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Capture the DB name here (once per Build() call) so every request scope
            // in _realHost hits the same in-memory database that was seeded below.
            // If the name were inside the options lambda it would re-evaluate Guid.NewGuid()
            // on every scope creation (DbContextOptions is Scoped by default), giving each
            // request an empty database and causing AuthorizationFilter to redirect forever.
            var dbName = $"E2EIntegration_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseInternalServiceProvider(internalServiceProvider)
                    .UseInMemoryDatabase(dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the standard in-process test host (used by base-class HttpClient helpers)
        var testHost = builder.Build();

        // Build a second host with a real Kestrel HTTPS listener on any available port.
        // UseHttps() uses the ASP.NET Core dev certificate so UseHttpsRedirection()
        // in Program.cs does not issue a redirect (request is already HTTPS).
        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel(o => o.ListenAnyIP(0, l => l.UseHttps())));

        _realHost = builder.Build();
        _realHost.Start();

        // Seed the anonymous categories required by AuthorizationFilter.
        // With an empty in-memory database every page redirects back to AnonymousIndex,
        // so we insert the minimum set of rows that the tests actually navigate to.
        using (var scope = _realHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Categories.AddRange(
                new Category { Id = 1, Name = "Dashboard", DisplayName = "Dashboard", IconPath = "", Controller = "DashBoard", Action = "AnonymousIndex", Role = Role.Anonymous, Order = 1 },
                new Category { Id = 2, Name = "Calendar",  DisplayName = "Calendar",  IconPath = "", Controller = "Calendar",  Action = "AnonymousIndex", Role = Role.Anonymous, Order = 2 },
                new Category { Id = 3, Name = "Forum",     DisplayName = "Forum",     IconPath = "", Controller = "Forum",     Action = nameof(BoardType.FreeForum),      Role = Role.Anonymous, Order = 3 }
            );
            db.SaveChanges();
        }

        // Discover the actual port that Kestrel chose.
        // Use www.localhost directly so AddRedirectToWww() in Program.cs never fires
        // (the host already has the www. prefix and no redirect is issued).
        var server = _realHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        var port = new Uri(addresses!.Addresses.Last()).Port;
        ServerBaseUrl = $"https://www.localhost:{port}";

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        _realHost?.Dispose();
        base.Dispose(disposing);
    }
}
