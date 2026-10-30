// Maroik.FileStorage — Program.cs
// Entry point for the file-storage microservice.
// Configures DI, CORS, and the HTTP pipeline, then starts Kestrel.

using System.Globalization;
using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Interfaces;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Settings;
using Maroik.FileStorage.Workers;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Logging.Console;

// The console line format, shared by the host's logger and the one that reports a failed start-up. SingleLine: a line break
// inside a logged value (an upload path …) is written as a space, so one entry stays one line and a value cannot start a forged
// entry of its own.
static void ConfigureConsole(SimpleConsoleFormatterOptions options)
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Disabled;
}

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(ConfigureConsole);
    builder.Logging.SetMinimumLevel(LogLevel.Information);
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware", LogLevel.Information);
    builder.Logging.AddFilter("System", LogLevel.Warning);

    // One entry per request: method, path (no query), status and duration — never headers or bodies.
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
            HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
        options.CombineLogs = true;
    });

    // Register MVC controllers (FileController and any future controllers).
    builder.Services.AddControllers();

    // Bind the upload size cap from the "ServerSetting" config section (see FileStorageSetting
    // for why it shares its name with Maroik.Website's ServerSetting).
    builder.Services.Configure<FileStorageSetting>(builder.Configuration.GetSection("ServerSetting"));

    // ClamAV daemon address lives in its own "Clamav" section, so bind it separately here rather
    // than nesting it under "ServerSetting".
    builder.Services.Configure<FileStorageSetting>(cfg =>
    {
        var clamavSection = builder.Configuration.GetSection("Clamav");
        cfg.ClamavHost = clamavSection["Host"];
        cfg.ClamavPort = Convert.ToInt32(clamavSection["Port"], CultureInfo.InvariantCulture);

        // The one directory every stored/served file must resolve within. In the container the
        // working dir and content root are both /app, and the upload volume mounts at /app/upload.
        if (string.IsNullOrWhiteSpace(cfg.StorageRootPath))
            cfg.StorageRootPath = Path.Combine(builder.Environment.ContentRootPath, "upload");
    });

    // Register the ClamAV virus-scan client (used by FileValidationService) and the validation service
    // FileController delegates to.
    builder.Services.AddScoped<IClamavClient, ClamavClient>();
    builder.Services.AddScoped<IFileValidationService, FileValidationService>();
    builder.Services.AddScoped<IFileManagerStorageService, FileManagerStorageService>();

    // The File Manager's scans run in the background (a large file takes longer than a request may wait): complete queues one,
    // FileManagerScanWorker runs it.
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IFileManagerScanQueue, FileManagerScanQueue>();
    builder.Services.AddHostedService<FileManagerScanWorker>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policyBuilder => { _ = policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
    });

    var app = builder.Build();

    app.UseHttpLogging(); // Log each HTTP request (method, path, status, duration)
    app.UseRouting();
    app.UseCors("AllowAll");

    // Map attribute-routed controllers (e.g. [Route("api/file")]).
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // The host's logger may not exist yet (or is already disposed); disposing this one flushes the entry before the process exits.
    using (var loggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole(ConfigureConsole)))
        loggerFactory.CreateLogger("Maroik.FileStorage").LogCritical(ex, "Maroik.FileStorage terminated unexpectedly");
    throw;
}
