using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Settings;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maroik.WebSite.Tests.Infrastructure;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for <c>Maroik.WebSite</c>.
///
/// Overrides applied for testing:
/// <list type="bullet">
///   <item>Configuration: empty cert paths (skips TLS hot-swap logic), test domain, short session timeout.</item>
///   <item>Database: replaces PostgreSQL with an EF Core InMemory database so tests need no real DB.</item>
///   <item>Base address: <c>https://www.localhost/</c> so the <c>UseHttpsRedirection</c> and
///       <c>AddRedirectToWww</c> middleware do not trigger additional redirects.</item>
/// </list>
/// </summary>
public class MaroikWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // -- Override configuration -------------------------------------------
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                // Skip TLS cert loading (DockerCertPath / DockerKeyPath empty → cert block skipped)
                ["ServerSetting:DockerCertPath"] = "",
                ["ServerSetting:DockerKeyPath"] = "",

                // Use a test domain; www prefix avoids AddRedirectToWww triggering
                ["ServerSetting:DomainName"] = "https://www.localhost",
                ["ServerSetting:SessionExpireMinutes"] = "60",
                ["ServerSetting:MaxLoginAttempt"] = "5",
                ["ServerSetting:NoticeMaturityDateDay"] = "7",
                ["ServerSetting:RsaPrivateKey"] = "",
                ["ServerSetting:RsaPublicKey"] = "",
                ["ServerSetting:RsaAlgorithm"] = "Rsa2",
                ["ServerSetting:FileStorageBaseUrl"] = "http://localhost:5001",

                // SMTP ?? not used in route tests
                ["ServerSetting:SmtpOptions:smtpUserName"] = "test",
                ["ServerSetting:SmtpOptions:smtpPassword"] = "test",
                ["ServerSetting:SmtpOptions:smtpHost"] = "localhost",
                ["ServerSetting:SmtpOptions:smtpPort"] = "25",
                ["ServerSetting:SmtpOptions:smtpSsl"] = "false",
                ["ServerSetting:SmtpOptions:fromEmail"] = "test@localhost",
                ["ServerSetting:SmtpOptions:fromFullName"] = "Test",
                ["ServerSetting:SmtpOptions:IsDefault"] = "true",

                // ClamAV ?? not used in route tests
                ["Clamav:Host"] = "localhost",
                ["Clamav:Port"] = "3310",

                // FileStorage
                ["FileStorage:BaseUrl"] = "http://localhost:5001",

                // DB ?? overridden below with InMemory, but a value is required to avoid null errors
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = ""
            }!);
        });

        // -- Replace PostgreSQL with InMemory --------------------------------
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations registered by AddRepositoryContext()
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbOptionsDescriptor != null) services.Remove(dbOptionsDescriptor);

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            // Use an explicit internal service provider so EF Core does not scan the app's
            // DI container and find both Npgsql (from AddRepositoryContext) and InMemory,
            // which would throw "multiple database providers registered".
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Capture the DB name once per factory instance so all request scopes share
            // the same in-memory database (DbContextOptions is Scoped, so Guid.NewGuid()
            // inside the lambda would produce a new database name on every request).
            var dbName = $"WebSiteIntegration_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseInternalServiceProvider(internalServiceProvider)
                    .UseInMemoryDatabase(dbName));

            // Replace Redis distributed cache with in-memory so tests run without a Redis instance.
            var distributedCacheDescriptors = services
                .Where(d => d.ServiceType == typeof(IDistributedCache))
                .ToList();
            foreach (var d in distributedCacheDescriptors) services.Remove(d);
            services.AddDistributedMemoryCache();

            // Replace Redis-backed Data Protection with ephemeral in-memory keys.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            // Program.cs reads DomainName before ConfigureAppConfiguration overrides take effect,
            // so Session/Antiforgery cookies end up with Domain=localhost (from appsettings.Development.json).
            // PostConfigure clears the domain so the CookieContainerHandler accepts cookies for www.localhost.
            services.PostConfigure<SessionOptions>(options => options.Cookie.Domain = null);
            services.PostConfigure<AntiforgeryOptions>(options => options.Cookie.Domain = null);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Categories.AddRange(
                new Category { Id = 1, Name = "Dashboard", DisplayName = "Dashboard", IconPath = "", Controller = "Dashboard", Action = "AnonymousIndex", Role = Role.Anonymous, Order = 1 },
                new Category { Id = 2, Name = "Calendar", DisplayName = "Calendar", IconPath = "", Controller = "Calendar", Action = "AnonymousIndex", Role = Role.Anonymous, Order = 2 },
                new Category { Id = 3, Name = "Forum", DisplayName = "Forum", IconPath = "", Controller = "Forum", Action = nameof(BoardType.FreeForum), Role = Role.Anonymous, Order = 3 },
                new Category { Id = 4, Name = "DriveAnon", DisplayName = "Drive", IconPath = "", Controller = "Drive", Action = "AnonymousIndex", Role = Role.Anonymous, Order = 4 },
                new Category { Id = 5, Name = "DriveAdmin", DisplayName = "Drive (Admin)", IconPath = "", Controller = "Drive", Action = "AdminIndex", Role = Role.Anonymous, Order = 5 },
                new Category { Id = 6, Name = "DriveUser", DisplayName = "Drive (User)", IconPath = "", Controller = "Drive", Action = "UserIndex", Role = Role.Anonymous, Order = 6 },
                new Category { Id = 7, Name = "Asset", DisplayName = "Asset", IconPath = "", Controller = "AccountBook", Action = "Asset", Role = Role.Anonymous, Order = 7 },
                new Category { Id = 8, Name = "Income", DisplayName = "Income", IconPath = "", Controller = "AccountBook", Action = "Income", Role = Role.Anonymous, Order = 8 },
                new Category { Id = 9, Name = "Expenditure", DisplayName = "Expenditure", IconPath = "", Controller = "AccountBook", Action = "Expenditure", Role = Role.Anonymous, Order = 9 },
                new Category { Id = 10, Name = "ManageAccount", DisplayName = "Accounts", IconPath = "", Controller = "Management", Action = "Account", Role = Role.Anonymous, Order = 10 },
                new Category { Id = 11, Name = "ManageMenu", DisplayName = "Menu", IconPath = "", Controller = "Management", Action = "Menu", Role = Role.Anonymous, Order = 11 },
                new Category { Id = 12, Name = "FixedIncome", DisplayName = "Fixed Income", IconPath = "", Controller = "Notice", Action = "FixedIncome", Role = Role.Anonymous, Order = 12 },
                new Category { Id = 13, Name = "FixedExpenditure", DisplayName = "Fixed Expend", IconPath = "", Controller = "Notice", Action = "FixedExpenditure", Role = Role.Anonymous, Order = 13 }
            );
            db.SaveChanges();
        }

        return host;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured to use <c>https://www.localhost/</c>
    /// as the base address. This prevents <c>UseHttpsRedirection</c> and <c>AddRedirectToWww</c>
    /// from issuing extra redirects during tests.
    /// </summary>
    public HttpClient CreateTestClient(bool followRedirects = false) =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://www.localhost/"),
            AllowAutoRedirect = followRedirects
        });
}
