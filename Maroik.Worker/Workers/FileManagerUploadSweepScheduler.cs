using Maroik.Core.Contract.Interfaces;

namespace Maroik.Worker.Workers;

/// <summary>
/// Removes the File Manager's abandoned uploads (<see cref="IFileManagerService.RemoveStaleUploadsAsync"/>) when the worker starts
/// and then once an hour, on the injected <see cref="TimeProvider"/>: an upload whose page sent no chunk and asked about no scan for an
/// hour is gone (closed, offline), and its reservation and staging file would otherwise wait for the same user's next upload. Each run
/// gets a scope of its own (the service uses the scoped DbContext) and sweeps at most <see cref="MaxAccountsPerRun"/> accounts, the
/// rest in the next run. A run that fails is logged and the next one goes ahead.
/// </summary>
public sealed class FileManagerUploadSweepScheduler(IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<FileManagerUploadSweepScheduler> logger)
    : BackgroundService
{
    /// <summary>The most accounts one run sweeps.</summary>
    public const int MaxAccountsPerRun = 100;

    /// <summary>How often the abandoned uploads are looked for.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IFileManagerService>().RemoveStaleUploadsAsync(MaxAccountsPerRun, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Removing the File Manager's abandoned uploads failed; trying again in an hour");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
