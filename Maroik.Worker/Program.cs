// Maroik.Worker - Program.cs
// Background service that consumes queued messages from RabbitMQ (currently: outgoing
// account emails) so the Website never blocks a request on an SMTP round-trip.
// `dotnet Maroik.Worker.dll redrive-email-dead-letters` instead moves the dead-lettered e-mails back
// into the e-mail queue and exits (run by an operator once the cause of the failures is fixed).

using Maroik.Worker;
using Maroik.Worker.Workers;

try
{
    if (args is [EmailDeadLetterRedriver.Command, ..])
    {
        using var redriveHost = WorkerHost.Build(args[1..]);
        await redriveHost.Services.GetRequiredService<EmailDeadLetterRedriver>().RedriveAsync(CancellationToken.None);
        return;
    }

    var host = WorkerHost.Build(args);
    await host.RunAsync();
}
catch (Exception ex)
{
    // The host's logger may not exist yet (or is already disposed); disposing this one flushes the entry before the process exits.
    using (var loggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole(WorkerHost.ConfigureConsole)))
        loggerFactory.CreateLogger("Maroik.Worker").LogCritical(ex, "Maroik.Worker terminated unexpectedly");
    throw;
}
