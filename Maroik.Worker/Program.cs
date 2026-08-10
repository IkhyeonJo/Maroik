// Maroik.Worker - Program.cs
// Background service that consumes queued messages from RabbitMQ (currently: outgoing
// account emails) so the Website never blocks a request on an SMTP round-trip.

using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Repository.Extensions;
using Maroik.Worker.Contracts;
using Maroik.Worker.Handlers;
using Maroik.Worker.Workers;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    #region ServerSettings (SMTP only — reuses the shared ServerSetting shape)

    builder.Services.Configure<ServerSetting>(cfg =>
    {
        var section = builder.Configuration.GetSection("ServerSetting");
        var smtp = section.GetSection("SmtpOptions");
        cfg.SmtpUserName = smtp["smtpUserName"];
        cfg.SmtpPassword = smtp["smtpPassword"];
        cfg.SmtpHost = smtp["smtpHost"];
        cfg.SmtpPort = Convert.ToInt32(smtp["smtpPort"]);
        cfg.SmtpSsl = Convert.ToBoolean(smtp["smtpSsl"]);
        cfg.FromEmail = smtp["fromEmail"];
        cfg.FromFullName = smtp["fromFullName"];
    });

    #endregion

    #region Data access (needed to update Account.Message on SMTP failure)

    builder.Services.AddRepositoryContext(builder.Configuration);
    builder.Services.AddRepositoryServices();

    #endregion

    builder.Services.AddScoped<IMailClient, MailClient>();
    builder.Services.AddScoped<IEmailMessageHandler, EmailMessageHandler>();

    string rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "amqp://guest:guest@localhost:5672/";
    builder.Services.AddSingleton<IConnectionFactory>(
        new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) });
    builder.Services.AddHostedService<EmailConsumerWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Maroik.Worker terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
