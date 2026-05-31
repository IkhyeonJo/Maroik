// Maroik.WebSite - Program.cs
// Entry point for the Maroik web application.
// Configures DI services (repositories, services, clients, session, antiforgery, CORS, localization),
// sets up hot-swap TLS certificate polling for Docker deployments, and builds the ASP.NET Core pipeline.

using Mapster;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Repository.Extensions;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Client.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Session;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
//using StackExchange.Redis;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

X509Certificate2 currentCert;
string previousCertHash;
string previousCertKeyHash;
object certLock = new();
var isReloading = false;
// ReSharper disable once TooWideLocalVariableScope
System.Timers.Timer certTimer;

// Serilog context logger for the certificate manager (used before host DI is available).
var certLogger = Log.ForContext("SourceContext", "CertificateManager");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

const string cookiePrefix = "__Secure-";

#region ServerSettings
// Capture config values needed before DI container is built (for middleware/Kestrel configuration)
var serverConfig = builder.Configuration.GetSection("ServerSetting");
var cfgDomainName = serverConfig["DomainName"] ?? string.Empty;
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

#region Mapster
var globalConfig = TypeAdapterConfig.GlobalSettings;
globalConfig.Scan(
    typeof(Maroik.WebSite.Mappings.MappingConfig).Assembly  // WebSite mappings (DTO <-> ViewModel)
);
builder.Services.AddSingleton(globalConfig);
#endregion

#region AddRepositories
builder.Services.AddRepositoryServices();
#endregion

#region AddServices
builder.Services.AddApplicationServices();
builder.Services.AddScoped<Maroik.WebSite.Services.IExcelExportService, Maroik.WebSite.Services.ExcelExportService>();
#endregion

#region AddClients
builder.Services.AddClientServices();
builder.Services.AddHttpClient();
#endregion

//#region REDIS
// NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
//builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
//{
//    var connectionString = builder.Configuration.GetConnectionString("Redis");
//    return ConnectionMultiplexer.Connect(connectionString!);
//});

// NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = builder.Configuration.GetConnectionString("Redis");
//    options.InstanceName = "MaroikWebSite";
//
//    // Additional settings for Redis connection stability
//    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
//    {
//        EndPoints = { builder.Configuration.GetConnectionString("Redis")! },
//        AbortOnConnectFail = false,
//        ConnectTimeout = 10000,
//        SyncTimeout = 10000,
//        AsyncTimeout = 10000,
//        ConnectRetry = 3,
//        ReconnectRetryPolicy = new ExponentialRetry(1000),
//        KeepAlive = 60
//    };
//});
//#endregion

#region AddSession
builder.Services.AddDistributedMemoryCache(); // Server Memory
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
    options.AddDefaultPolicy(
        corsPolicyBuilder =>
        {
            _ = corsPolicyBuilder.WithOrigins(cfgDomainName.Replace("/", ""));
        });
});
#endregion

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache(); // For IMemoryCache DI
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
builder.Services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(
    opt =>
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

builder.Services.AddHealthChecks();

builder.Services.AddControllersWithViews(options =>
{
#pragma warning disable CA2263
    _ = options.Filters.Add(typeof(AuthorizationFilter)); // An instance
#pragma warning restore CA2263
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // Globally enables token validation for all requests except for GET, HEAD, OPTIONS, and TRACE
});

var app = builder.Build();

#region Hot Swap Cert
string ComputeFileHash(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = new FileStream(
        filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite,
        bufferSize: 8192,
        FileOptions.SequentialScan);

    var hashBytes = sha256.ComputeHash(stream);
    return Convert.ToHexString(hashBytes); // .NET 5+
}
// Certificate loading function definition using X509CertificateLoader
X509Certificate2 LoadCertificate(string certPath, string keyPath)
{
    try
    {
        var realCertPem = "";
        var realKeyPem = "";
        
        // Read certificate and key as string (pem format)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var linkCertPem = File.ReadAllText(certPath);
            var linkKeyPem = File.ReadAllText(keyPath);
            
            var realCertName = Path.GetFileName(linkCertPem);
            var realKeyName = Path.GetFileName(linkKeyPem);

            var realCertPath = certPath.Replace("/live/", "/archive/").Replace("cert.pem", realCertName);
            var realKeyPath = keyPath.Replace("/live/", "/archive/").Replace("privkey.pem", realKeyName);

            realCertPem = File.ReadAllText(realCertPath);
            realKeyPem = File.ReadAllText(realKeyPath);
            
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            realCertPem = File.ReadAllText(certPath);
            realKeyPem = File.ReadAllText(keyPath);
        }
        
        // Create the certificate using X509Certificate2.CreateFromPem
        var cert = X509Certificate2.CreateFromPem(realCertPem);

        // Load private key from PEM manually
        using var ecdsaPrivateKey = ECDsa.Create();
        ecdsaPrivateKey.ImportFromPem(realKeyPem);

        // Combine certificate plus private key
        var certWithKey = cert.CopyWithPrivateKey(ecdsaPrivateKey);

        return certWithKey;
    }
    catch (Exception ex)
    {
        certLogger.Error(ex, "Failed to load certificate from {CertPath} and {KeyPath}", certPath, keyPath);
        throw;
    }
}

if (!string.IsNullOrEmpty(cfgDockerCertPath) && !string.IsNullOrEmpty(cfgDockerKeyPath))
{
    // Load certificate
    currentCert = LoadCertificate(cfgDockerCertPath, cfgDockerKeyPath);
    previousCertHash = ComputeFileHash(cfgDockerCertPath);
    previousCertKeyHash = ComputeFileHash(cfgDockerKeyPath);
    
    // Configure Kestrel SSL settings
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificateSelector = (_, _) =>
            {
                lock (certLock)
                {
                    return currentCert;
                }
            };
        });
    });

    certTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds); // for debug [TimeSpan.FromSeconds(5)]
    certTimer.AutoReset = true;

    var logger = app.Logger;
    certTimer.Elapsed += (_, _) =>
    {
        if (isReloading) return;
        isReloading = true;

        try
        {
            var currentCertHash = ComputeFileHash(cfgDockerCertPath);
            var currentCertKeyHash = ComputeFileHash(cfgDockerKeyPath);
            // ReSharper disable once InvertIf
            if (currentCertHash != previousCertHash &&
                currentCertKeyHash != previousCertKeyHash)
            {
                logger.LogInformation("Certificate file changed, reloading...");

                var newCert = LoadCertificate(cfgDockerCertPath, cfgDockerKeyPath);

                lock (certLock)
                {
                    currentCert.Dispose();
                    currentCert = newCert;
                    previousCertHash = currentCertHash;
                    previousCertKeyHash = currentCertKeyHash;
                }
                logger.LogInformation("Certificate reloaded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload certificate");
        }
        finally
        {
            isReloading = false;
        }
    };

    certTimer.Start();
    certLogger.Information("Certificate polling started.");
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

    #region Security Headers [https://blog.elmah.io/the-asp-net-core-security-headers-guide/]
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Xss-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("Content-Security-Policy", $"img-src 'self' blob:; connect-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'none'; report-uri {cfgDomainName}; form-action 'self'; frame-ancestors 'none'"); // [https://csper.io/blog/no-more-unsafe-inline] [https://csp-evaluator.withgoogle.com/?csp=https://www.maroik.com]
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
    pattern: "{controller=DashBoard}/{action=AnonymousIndex}/{id?}");
app.UseMiddleware<RoleBasedStaticFileMiddleware>();
app.MapStaticAssets();

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Maroik.WebSite terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
