// Maroik.Website - Program.cs
// Entry point for the Maroik web application.
// Configures DI services (repositories, services, clients, session, antiforgery, CORS, localization),
// sets up hot-swap TLS certificate polling for Docker deployments, and builds the ASP.NET Core pipeline.

using System.Globalization;
using Maroik.Core.Client.Extensions;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Repository.Extensions;
using Maroik.Core.Service.Extensions;
using Maroik.Website.Extensions;
using Maroik.Website.Filters;
using Maroik.Website.Middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Session;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

// Bootstrap logger captures startup errors before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    WebApplication? app = null; // assigned after Build() below; captured by the Kestrel cert selector closure

    builder.Host.UseSerilog((_, _, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    const string cookiePrefix = "__Secure-";

    #region ServerSettings

    // Capture config values needed before DI container is built (for middleware/Kestrel configuration)
    var serverConfig = builder.Configuration.GetSection("ServerSetting");
    var cfgDomainName = serverConfig["DomainName"] ?? "";
    var cfgSessionExpireMinutes = Convert.ToInt32(serverConfig["SessionExpireMinutes"]);
    var cfgDockerCertPath = serverConfig["DockerCertPath"];
    var cfgDockerKeyPath = serverConfig["DockerKeyPath"];

    // Register ServerSetting as IOptions<ServerSetting> for DI
    builder.Services.Configure<ServerSetting>(cfg =>
    {
        var section = builder.Configuration.GetSection("ServerSetting");
        cfg.DomainName = section["DomainName"];
        cfg.MaxLoginAttempt = Convert.ToByte(section["MaxLoginAttempt"]);
        cfg.SessionExpireMinutes = Convert.ToInt32(section["SessionExpireMinutes"]);
        var smtp = section.GetSection("SmtpOptions");
        cfg.SmtpUserName = smtp["smtpUserName"];
        cfg.SmtpPassword = smtp["smtpPassword"];
        cfg.SmtpHost = smtp["smtpHost"];
        cfg.SmtpPort = Convert.ToInt32(smtp["smtpPort"]);
        cfg.SmtpSsl = Convert.ToBoolean(smtp["smtpSsl"]);
        cfg.FromEmail = smtp["fromEmail"];
        cfg.FromFullName = smtp["fromFullName"];
        cfg.IsDefault = Convert.ToBoolean(smtp["IsDefault"]);
        cfg.MaxAttachedFileSizeBytes = Convert.ToInt64(section["MaxAttachedFileSizeBytes"]);
        cfg.NoticeMaturityDateDay = Convert.ToInt32(section["NoticeMaturityDateDay"]);
        cfg.DockerCertPath = section["DockerCertPath"];
        cfg.DockerKeyPath = section["DockerKeyPath"];
        cfg.RsaPrivateKey = section["RsaPrivateKey"];
        cfg.RsaPublicKey = section["RsaPublicKey"];
        var rsaAlgorithmString = section["RsaAlgorithm"] ?? "Rsa2";
        cfg.RsaAlgorithm = Enum.TryParse<RsaType>(rsaAlgorithmString, out var rsaType) ? rsaType : RsaType.Rsa2;
        cfg.ClamavHost = builder.Configuration.GetSection("Clamav")["Host"];
        cfg.ClamavPort = Convert.ToInt32(builder.Configuration.GetSection("Clamav")["Port"]);
        cfg.FileStorageBaseUrl = builder.Configuration.GetSection("FileStorage")["BaseUrl"];
    });

    #endregion

    #region AddDbContext

    builder.Services.AddRepositoryContext(builder.Configuration);

    #endregion

    #region AddRepositories

    builder.Services.AddRepositoryServices();

    #endregion

    #region AddServices

    builder.Services.AddApplicationServices();
    builder.Services
        .AddScoped<Maroik.Website.Contracts.IExcelExportService, Maroik.Website.Services.ExcelExportService>()
        .AddScoped<Maroik.Website.Contracts.ISessionService, Maroik.Website.Services.SessionService>();

    // Registered here (before Build()) so it goes through DI like the other services. ConfigureKestrel
    // below must also run before Build() - the service collection becomes read-only once Build() runs.
    // The selector closure captures `app`, which is only assigned after Build() (see below), but the
    // selector itself isn't invoked until the first TLS handshake, long after that.
    if (!string.IsNullOrEmpty(cfgDockerCertPath) && !string.IsNullOrEmpty(cfgDockerKeyPath))
    {
        builder.Services.AddSingleton<Maroik.Website.Contracts.ICertificateManager>(sp =>
            new Maroik.Website.Services.CertificateManager(
                cfgDockerCertPath,
                cfgDockerKeyPath,
                sp.GetRequiredService<ILogger<Maroik.Website.Services.CertificateManager>>()));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(https =>
            {
                // ReSharper disable once AccessToModifiedClosure - `app` is assigned exactly once
                // (right after Build(), below) before this selector can ever be invoked.
                https.ServerCertificateSelector = (_, _) =>
                    app!.Services.GetRequiredService<Maroik.Website.Contracts.ICertificateManager>()
                        .SelectCertificate();
            });
        });
    }

    #endregion

    #region AddClients

    string rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "amqp://guest:guest@localhost:5672/";
    builder.Services.AddClientServices(rabbitMqConnectionString);
    builder.Services.AddHttpClient();

    #endregion

    #region Valkey / Session / DataProtection
    // Valkey is wire-protocol compatible with Redis, so the StackExchange.Redis client
    // (there is no separate "StackExchange.Valkey" package) connects to it unmodified.

    var valkeyConnectionString = builder.Configuration.GetConnectionString("Valkey");
    IConnectionMultiplexer? valkey = string.IsNullOrEmpty(valkeyConnectionString)
        ? null
        : ConnectionMultiplexer.Connect(valkeyConnectionString);

    if (valkey != null)
    {
        builder.Services.AddSingleton(valkey);
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(valkey);
            options.InstanceName = "MaroikWebsite:";
        });
        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(valkey, "MaroikWebsite:DataProtection-Keys")
            .SetApplicationName("Maroik");
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(cfgSessionExpireMinutes);
        options.Cookie.Name = $"{cookiePrefix}{SessionDefaults.CookieName}";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Domain = cfgDomainName.Replace("https://", "").Replace("/", "");
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    #endregion

    #region AddAntiforgery

    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.Name = $"{cookiePrefix}{AntiforgeryOptions.DefaultCookiePrefix}";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Domain = cfgDomainName.Replace("https://", "").Replace("/", "");
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

    #endregion

    #region AddCors

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsPolicyBuilder =>
        {
            _ = corsPolicyBuilder.WithOrigins(cfgDomainName.Replace("/", ""));
        });
    });

    #endregion

    builder.Services.AddHttpContextAccessor();
    // builder.Services.AddMemoryCache(); // For IMemoryCache DI - unused; IDistributedCache(Valkey) used instead for scale-out
    builder.Services.AddResponseCaching(); // For HTTP Header Setting

    builder.Services.AddHsts(options => // https://blog.elmah.io/the-asp-net-core-security-headers-guide/
    {
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
        options.Preload = true;
        options.ExcludedHosts.Add(cfgDomainName.Replace("https://", "").Replace("/", ""));
        options.ExcludedHosts.Add(cfgDomainName.Replace("https://", "").Replace("/", "").Replace("www.", ""));
    });

    #region Multi-Language

    builder.Services.AddLocalization(opt => { opt.ResourcesPath = "Resources"; });
    builder.Services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
        .AddDataAnnotationsLocalization();

    builder.Services.Configure<RequestLocalizationOptions>(opt =>
    {
        List<CultureInfo> supportedCultures =
        [
            new("en-US"),
            new("ko-KR")
        ];
        opt.DefaultRequestCulture = new RequestCulture("en-US");
        opt.SupportedCultures = supportedCultures;
        opt.SupportedUICultures = supportedCultures;
    });

    #endregion

    // Custom checks (not the AspNetCore health checks.* NuGet packages) so /health actually reflects
    // whether the app can reach its dependencies, for the CI/CD post-deploy health check.
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddCheck<Maroik.Core.Repository.HealthChecks.DatabaseHealthCheck>("database")
        .AddTypeActivatedCheck<Maroik.Core.Client.HealthChecks.RabbitMqHealthCheck>(
            "rabbitmq", args: [rabbitMqConnectionString]);

    if (valkey != null)
    {
        healthChecksBuilder.AddCheck<Maroik.Website.HealthChecks.ValkeyHealthCheck>("valkey");
    }

    builder.Services.AddControllersWithViews(options =>
    {
#pragma warning disable CA2263
        _ = options.Filters.Add(typeof(AuthorizationFilter));
        _ = options.Filters.Add(typeof(ViewBagPopulatorFilter));
#pragma warning restore CA2263
        options.Filters.Add(
            new AutoValidateAntiforgeryTokenAttribute()); // Globally enables token validation for all requests except for GET, HEAD, OPTIONS, and TRACE
    });

    app = builder.Build();

    #region Hot Swap Cert

    if (!string.IsNullOrEmpty(cfgDockerCertPath) && !string.IsNullOrEmpty(cfgDockerKeyPath))
    {
        var certManager = app.Services.GetRequiredService<Maroik.Website.Contracts.ICertificateManager>();

        var certTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds)
        {
            AutoReset = true
        };
        certTimer.Elapsed += (_, _) => certManager.TryReload();
        certTimer.Start();

        app.Logger.LogInformation("Certificate polling started.");
    }

    #endregion

    //1.Exception / error handling

    //When the app is running in the development environment:
    //The developer exception page middleware (UseDeveloperExceptionPage) reports app runtime errors.
    //The database error page middleware reports database runtime errors.
    //When the app is running in the production environment:
    //The exception handler middleware (UseExceptionHandler) catches exceptions thrown in the following middleware.
    //The HSTS (HTTP Strict Transport Security Protocol) middleware (UseHsts) adds the Strict-Transport-Security header.

    //if (!app.Environment.IsDevelopment())
    //{
    //    app.UseDeveloperExceptionPage();
    //}
    //else
    //{
    //    app.UseExceptionHandler("/Exception/Error");
    //    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //    app.UseHsts();
    //}

    app.UseCookiePolicy(new CookiePolicyOptions
    {
        HttpOnly = HttpOnlyPolicy.Always,
        Secure = CookieSecurePolicy.Always,
    });

    app.UseExceptionHandler("/Exception/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // /health bypasses HTTPS redirect and www-redirect so it is reachable on HTTP port 80
    // from the deployment server (used by the CI/CD post-deploy health check).
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        pipeline =>
        {
            pipeline.UseHttpsRedirection();
            pipeline.UseRewriter(new RewriteOptions().AddRedirectToWww());
        });

    app.UseSerilogRequestLogging(); // Log each HTTP request (method, path, status, duration) via Serilog
    app.UseRouting(); // 5. Routing middleware for routing requests (UseRouting)
    app.UseCors();

    #region Login

    app.UseSession(); // 6. Session middleware (UseSession) establishes and maintains session state. If the app uses session state, call Session Middleware after Cookie Policy Middleware and before MVC Middleware.
    app.UseAuthentication(); // 7. Authentication middleware (UseAuthentication) attempts to authenticate the user before they're allowed access to secure resources.
    app.UseAuthorization(); // 8. Authorization middleware (UseAuthorization) authorizes a user to access secure resources.

    #endregion

    #region Default HTTP Header Setting

    //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
    app.UseResponseCaching();

    #region Apply the following code to all pages. After logout, pressing back button shows logged-in page, so cache is disabled

    //<META http-equiv="Expires" content="-1">
    //<META http-equiv="Pragma" content="no-cache">
    //<META http-equiv="Cache-Control" content="No-Cache">

    //The code below is equivalent to the metadata content that goes into the HTML Header above.

    app.Use(async (context, next) =>
    {
        context.Response.GetTypedHeaders().CacheControl =
            new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
            {
                //Public = true,
                //MaxAge = TimeSpan.FromSeconds(10),
                NoCache = true, // For resolve logout back button problem
                NoStore = true // For resolve logout back button problem
            };
        context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
            (string[])["Accept-Encoding"];

        #region Security Headers [https: //blog.elmah.io/the-asp-net-core-security-headers-guide/]

        context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
        context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        context.Response.Headers.Append("Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
        context.Response.Headers.Append("Content-Security-Policy",
            $"img-src 'self' blob:; connect-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'none'; report-uri {cfgDomainName}; form-action 'self'; frame-ancestors 'none'"); // [https://csper.io/blog/no-more-unsafe-inline] [https://csp-evaluator.withgoogle.com/?csp=https://www.maroik.com]

        #endregion

        await next();
    });

    #endregion

    #endregion

    #region Multi-Language

    string[] supportedCultures = ["en-US", "ko-KR"];
    var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    app.UseRequestLocalization(localizationOptions);

    #endregion

    app.MapHealthChecks("/health");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=AnonymousIndex}/{id?}");
    // Must run immediately before MapStaticAssets so it can 403 role-restricted /admin and /user
    // static file paths before the static file middleware gets a chance to serve them.
    app.UseMiddleware<RoleBasedStaticFileMiddleware>();
    app.MapStaticAssets();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Maroik.Website terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
