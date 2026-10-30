// Maroik.FileStorage — Program.cs
// Entry point for the file-storage microservice.
// Configures DI, CORS, and the HTTP pipeline, then starts Kestrel.

using System.Globalization;
using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Constants;
using Maroik.FileStorage.Interfaces;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Settings;
using Maroik.FileStorage.Workers;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: LogTemplates.Console, formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: LogTemplates.Console, formatProvider: CultureInfo.InvariantCulture));

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

    app.UseSerilogRequestLogging(); // Log each HTTP request (method, path, status, duration) via Serilog
    app.UseRouting();
    app.UseCors("AllowAll");

    // Map attribute-routed controllers (e.g. [Route("api/file")]).
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Maroik.FileStorage terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
