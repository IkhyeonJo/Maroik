using Maroik.Core.Repository.Contracts;
using Maroik.Core.Repository.Data;
using Maroik.Core.Repository.Repositories;
using Maroik.Core.Common.Utilities;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Contracts.Services;
using Maroik.WebSite.Contracts.Clients;
using Maroik.WebSite.Clients;
using Maroik.WebSite.Services;
using Maroik.WebSite.Utilities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using StackExchange.Redis;

X509Certificate2 currentCert;
string previousCertHash;
string previousCertKeyHash;
object certLock = new();
var isReloading = false;
// ReSharper disable once TooWideLocalVariableScope
System.Timers.Timer certTimer;

var builder = WebApplication.CreateBuilder(args);

const string cookiePrefix = "__Secure-";

#region ServerSettings
ServerSetting.DomainName = builder.Configuration.GetSection("ServerSetting")["DomainName"];
ServerSetting.MaxLoginAttempt = Convert.ToByte(builder.Configuration.GetSection("ServerSetting")["MaxLoginAttempt"]);
ServerSetting.SessionExpireMinutes = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["SessionExpireMinutes"]);
ServerSetting.SmtpUserName = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpUserName"];
ServerSetting.SmtpPassword = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpPassword"];
ServerSetting.SmtpHost = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpHost"];
ServerSetting.SmtpPort = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpPort"]);
ServerSetting.SmtpSSL = Convert.ToBoolean(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["smtpSSL"]);
ServerSetting.FromEmail = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["fromEmail"];
ServerSetting.FromFullName = builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["fromFullName"];
ServerSetting.IsDefault = Convert.ToBoolean(builder.Configuration.GetSection("ServerSetting").GetSection("SmtpOptions")["IsDefault"]);
ServerSetting.NoticeMaturityDateDay = Convert.ToInt32(builder.Configuration.GetSection("ServerSetting")["NoticeMaturityDateDay"]);
ServerSetting.DockerCertPath = builder.Configuration.GetSection("ServerSetting")["DockerCertPath"];
ServerSetting.DockerKeyPath = builder.Configuration.GetSection("ServerSetting")["DockerKeyPath"];
ServerSetting.RSAPrivateKey = builder.Configuration.GetSection("ServerSetting")["RSAPrivateKey"];
ServerSetting.RSAPublicKey = builder.Configuration.GetSection("ServerSetting")["RSAPublicKey"];
string rsaAlgorithmString = builder.Configuration.GetSection("ServerSetting")["RSAAlgorithm"] ?? "RSA2";
ServerSetting.RSAAlgorithm = Enum.TryParse<RSAType>(rsaAlgorithmString, out var rsaType) ? rsaType : RSAType.RSA2;
#endregion

#region AddDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(0) // Set to 0 to disable
    ).EnableSensitiveDataLogging()
);
#endregion

#region AddRepositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IIncomeRepository, IncomeRepository>();
builder.Services.AddScoped<IExpenditureRepository, ExpenditureRepository>();
builder.Services.AddScoped<IFixedIncomeRepository, FixedIncomeRepository>();
builder.Services.AddScoped<IFixedExpenditureRepository, FixedExpenditureRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardAttachedFileRepository, BoardAttachedFileRepository>();
builder.Services.AddScoped<IBoardCommentRepository, BoardCommentRepository>();
builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
builder.Services.AddScoped<IOtherCalendarRepository, OtherCalendarRepository>();
builder.Services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
builder.Services.AddScoped<ICalendarEventAttachedFileRepository, CalendarEventAttachedFileRepository>();
builder.Services.AddScoped<ICalendarEventReminderRepository, CalendarEventReminderRepository>();
builder.Services.AddScoped<ICalendarSharedRepository, CalendarSharedRepository>();
#endregion

#region AddServices
builder.Services.AddScoped<IPasswordService, PasswordService>();
#endregion

#region AddClients
builder.Services.AddScoped<IMailClient, MailClient>();
builder.Services.AddScoped<IFileClient, FileClient>();
builder.Services.AddScoped<IClamavClient, ClamavClient>();
builder.Services.AddHttpClient();
#endregion

//#region REDIS
// NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
//builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
//{
//    var connectionString = builder.Configuration.GetConnectionString("Redis");
//    return ConnectionMultiplexer.Connect(connectionString!);
//});
//#endregion

#region AddSession
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

builder.Services.AddDistributedMemoryCache(); // Server Memory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(ServerSetting.SessionExpireMinutes);
    options.Cookie.Name = $"{cookiePrefix}{SessionDefaults.CookieName}";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Domain = ServerSetting.DomainName?.Replace("https://", "").Replace("/", "");
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
    options.Cookie.Domain = ServerSetting.DomainName?.Replace("https://", "").Replace("/", "");
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
            _ = corsPolicyBuilder.WithOrigins(ServerSetting.DomainName?.Replace("/", "") ?? string.Empty);
        });
});
#endregion

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddResponseCaching(); // For HTTP Header Setting 

builder.Services.AddHsts(options => // https://blog.elmah.io/the-asp-net-core-security-headers-guide/
{
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
    options.Preload = true;
    options.ExcludedHosts.Add(ServerSetting.DomainName?.Replace("https://", "").Replace("/", ""));
    options.ExcludedHosts.Add(ServerSetting.DomainName?.Replace("https://", "").Replace("/", "").Replace("www.", ""));
});

#region Multi-Language
builder.Services.AddLocalization(opt => { opt.ResourcesPath = "Resources"; });
builder.Services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(
    opt =>
    {
        List<CultureInfo> supportedCultures =
        [
                        new CultureInfo("en-US"),
                        new CultureInfo("ko-KR")
        ];
        opt.DefaultRequestCulture = new RequestCulture("en-US");
        opt.SupportedCultures = supportedCultures;
        opt.SupportedUICultures = supportedCultures;
    });
#endregion

builder.Services.AddControllersWithViews(options =>
{
    _ = options.Filters.Add(typeof(AuthorizationFilter)); // An instance
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // Globally enables token validation for all requests except for GET, HEAD, OPTIONS, and TRACE
});

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
        Console.WriteLine($"Failed to load certificate from {certPath} and {keyPath}: {ex.Message}");
        throw;
    }
}

if (!string.IsNullOrEmpty(ServerSetting.DockerCertPath) && !string.IsNullOrEmpty(ServerSetting.DockerKeyPath))
{
    // Load certificate
    currentCert = LoadCertificate(ServerSetting.DockerCertPath, ServerSetting.DockerKeyPath);
    previousCertHash = ComputeFileHash(ServerSetting.DockerCertPath);
    previousCertKeyHash = ComputeFileHash(ServerSetting.DockerKeyPath);
    
    // Configure Kestrel SSL settings
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificateSelector = (_, _) =>
            {
                lock (certLock)
                {
                    return currentCert!;
                }
            };
        });
    });

    certTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds); // for debug [TimeSpan.FromSeconds(5)]
    certTimer.AutoReset = true;

    certTimer.Elapsed += (_, _) =>
    {
        if (isReloading) return;
        isReloading = true;

        try
        {
            var currentCertHash = ComputeFileHash(ServerSetting.DockerCertPath);
            var currentCertKeyHash = ComputeFileHash(ServerSetting.DockerKeyPath);
            // ReSharper disable once InvertIf
            if (currentCertHash != previousCertHash &&
                currentCertKeyHash != previousCertKeyHash)
            {
                Console.WriteLine("Certificate file, reloading...");

                var newCert = LoadCertificate(ServerSetting.DockerCertPath, ServerSetting.DockerKeyPath);

                lock (certLock)
                {
                    currentCert?.Dispose();
                    currentCert = newCert;
                    previousCertHash = currentCertHash;
                    previousCertKeyHash = currentCertKeyHash;
                }
                Console.WriteLine("Certificate reloaded successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reload certificate: {ex.Message}");
        }
        finally
        {
            isReloading = false;
        }
    };

    certTimer.Start();
    Console.WriteLine("Certificate polling started.");
}
#endregion

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true); // https://stackoverflow.com/questions/69961449/net6-and-datetime-problem-cannot-write-datetime-with-kind-utc-to-postgresql-ty

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
app.UseHttpsRedirection();

app.UseRewriter(new RewriteOptions()
  .AddRedirectToWww());

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

//The code below is equivalent to the META data content that goes into the HTML Header above.

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
        new [] { "Accept-Encoding" };

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
    context.Response.Headers.Append("Content-Security-Policy", $"img-src 'self' blob:; connect-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'none'; report-uri {ServerSetting.DomainName}; form-action 'self'; frame-ancestors 'none'"); // [https://csper.io/blog/no-more-unsafe-inline] [https://csp-evaluator.withgoogle.com/?csp=https://www.maroik.com]
    #endregion
    await next();
});
#endregion
#endregion

#region Multi-Language
string[] supportedCultures = ["en-US", "ko-KR"];
RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
#endregion

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DashBoard}/{action=AnonymousIndex}/{id?}");
app.UseMiddleware<RoleBasedStaticFileMiddleware>();
app.MapStaticAssets();

app.Run();