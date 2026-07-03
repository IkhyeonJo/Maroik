// Maroik.FileStorage — Program.cs
// Entry point for the file-storage microservice.
// Configures DI, Swagger/OpenAPI, CORS, and the HTTP pipeline, then starts Kestrel.

using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Maroik.FileStorage.Contracts;
using Maroik.FileStorage.Services;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

// Bootstrap logger captures startup errors before the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    // Register MVC controllers (FileController and any future controllers).
    builder.Services.AddControllers();

    // Register the ClamAV virus-scan client so FileController can inject it.
    builder.Services.AddScoped<IClamavClient, ClamavClient>();
    builder.Services.AddScoped<IFileValidationService, FileValidationService>();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    // Exposes the OpenAPI metadata endpoint consumed by SwaggerUI.
    builder.Services.AddEndpointsApiExplorer();

    // Configure Swagger document generation (title and version shown in the UI).
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Maroik.FileStorage", Version = "v1" });
    });

    // Open CORS policy — the file-storage service is called from the Website origin only in production;
    // the permissive policy here is intentional for local development convenience.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policyBuilder => { _ = policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
    });

    var app = builder.Build();

    // Show detailed error pages in development; production errors propagate as 500 responses.
    if (app.Environment.IsDevelopment())
    {
        _ = app.UseDeveloperExceptionPage();
    }

    // Mount the Swagger JSON endpoint and the interactive Swagger UI at the app root.
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./swagger/v1/swagger.json", "Maroik.FileStorage");
        c.DocumentTitle = "Maroik.FileStorage";
        c.RoutePrefix = string.Empty; // Serve Swagger UI at "/" instead of "/swagger".
    });

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
}
finally
{
    Log.CloseAndFlush();
}
