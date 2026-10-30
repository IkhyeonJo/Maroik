using System.Globalization;
using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Repository.Extensions;
using Maroik.Core.Service.Services;
using Maroik.Worker.Handlers;
using Maroik.Worker.Interfaces;
using Maroik.Worker.Workers;
using Microsoft.Extensions.Logging.Console;
using RabbitMQ.Client;

namespace Maroik.Worker;

/// <summary>
/// Composes the worker's host: settings, data access, mail client, message handler and the RabbitMQ consumer.
/// Kept out of <c>Program.cs</c> so tests can build and run the very same host (against a real broker) instead of a copy.
/// </summary>
internal static class WorkerHost
{
    /// <summary>Broker address used when <c>ConnectionStrings:RabbitMQ</c> is not configured (a local development broker).</summary>
    private const string DefaultRabbitMqConnectionString = "amqp://guest:guest@localhost:5672/";

    /// <summary>
    /// The console line format, shared with the logger <c>Program.cs</c> reports a failed start-up with. SingleLine: a line break
    /// inside a logged value (a recipient address from a queued message …) is written as a space, so one entry stays one line and
    /// a value cannot start a forged entry of its own. The scopes carry a message's delivery tag and correlation id.
    /// </summary>
    internal static void ConfigureConsole(SimpleConsoleFormatterOptions options)
    {
        options.SingleLine = true;
        options.IncludeScopes = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.ColorBehavior = LoggerColorBehavior.Disabled;
    }

    /// <summary>Builds (does not start) the worker host from the command line / environment / appsettings in <paramref name="args"/>.</summary>
    internal static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(ConfigureConsole);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);

        #region ServerSettings (SMTP, the reminders' and the File Manager sweep's — reuses the shared ServerSetting shape)

        builder.Services.Configure<ServerSetting>(cfg =>
        {
            var section = builder.Configuration.GetSection("ServerSetting");
            var smtp = section.GetSection("SmtpOptions");
            cfg.SmtpUserName = smtp["smtpUserName"];
            cfg.SmtpPassword = smtp["smtpPassword"];
            cfg.SmtpHost = smtp["smtpHost"];
            cfg.SmtpPort = Convert.ToInt32(smtp["smtpPort"], CultureInfo.InvariantCulture);
            cfg.SmtpSsl = Convert.ToBoolean(smtp["smtpSsl"]);
            cfg.FromEmail = smtp["fromEmail"];
            cfg.FromFullName = smtp["fromFullName"];
            // The calendar reminders' link base and the account they never go to (CalendarReminderDispatchService).
            cfg.DomainName = section["DomainName"];
            cfg.DemoAccountEmail = section["DemoAccountEmail"];
            // The file storage the File Manager sweep aborts abandoned uploads in (the same key as the website's).
            cfg.FileStorageBaseUrl = builder.Configuration["FileStorage:BaseUrl"];
        });

        #endregion

        #region Data access (needed by IAccountMailStatusService to update Account.Message on SMTP failure)

        builder.Services.AddRepositoryContext(builder.Configuration);
        builder.Services.AddRepositoryServices();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IAccountMailStatusService, AccountMailStatusService>();

        #endregion

        builder.Services.AddScoped<IMailClient, MailClient>();
        builder.Services.AddScoped<IEmailMessageHandler, EmailMessageHandler>();

        string rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
            ?? DefaultRabbitMqConnectionString;
        builder.Services.AddSingleton<IConnectionFactory>(
            new ConnectionFactory
            {
                Uri = new Uri(rabbitMqConnectionString),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = "maroik-worker-email-consumer"
            });
        builder.Services.AddHostedService<EmailConsumerWorker>();

        // Calendar reminders: once a minute the scheduler sends the due ones, queued as e-mails on the same broker (consumed by
        // EmailConsumerWorker above, so they share its retry and dead-letter path). The publisher keeps its own connection.
        builder.Services.AddSingleton<IEmailPublisher>(sp => new RabbitMqEmailPublisher(
            new ConnectionFactory
            {
                Uri = new Uri(rabbitMqConnectionString),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = "maroik-worker-reminder-publisher"
            },
            sp.GetRequiredService<ILogger<RabbitMqEmailPublisher>>()));
        builder.Services.AddScoped<ICalendarReminderDispatchService, CalendarReminderDispatchService>();
        builder.Services.AddHostedService<CalendarReminderScheduler>();
        builder.Services.AddSingleton<EmailDeadLetterRedriver>();

        // File Manager: once an hour the scheduler removes the abandoned uploads — their rows here, their staging files in file storage.
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IFileClient, FileClient>();
        builder.Services.AddScoped<IFileManagerService, FileManagerService>();
        builder.Services.AddHostedService<FileManagerUploadSweepScheduler>();

        return builder.Build();
    }
}
