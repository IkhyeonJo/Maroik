using Maroik.Core.Contract.Interfaces;
using Maroik.Worker.Tests.Fixtures;
using Maroik.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// Unit tests for <see cref="FileManagerUploadSweepScheduler"/>: it removes the File Manager's abandoned uploads when it starts and then
/// once an hour (on the injected clock), each run in a scope of its own and bounded to
/// <see cref="FileManagerUploadSweepScheduler.MaxAccountsPerRun"/>; a failing run is logged and the next hour's run still happens.
/// </summary>
public class FileManagerUploadSweepSchedulerTests
{
    private readonly FakeFileManagerService _files = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero));
    private readonly FakeLogger<FileManagerUploadSweepScheduler> _logger = new();

    /// <summary>The scheduler over a container holding the fake File Manager service.</summary>
    private FileManagerUploadSweepScheduler Sut()
    {
        var services = new ServiceCollection();
        services.AddScoped<IFileManagerService>(_ => _files);
        return new FileManagerUploadSweepScheduler(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _clock, _logger);
    }

    /// <summary>Waits (real time, bounded) until the service has been called <paramref name="times"/> times.</summary>
    private async Task CalledAsync(int times)
    {
        for (int i = 0; i < 200 && _files.RemoveStaleUploadsAsyncCalls.Count < times; i++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(times, _files.RemoveStaleUploadsAsyncCalls.Count);
    }

    /// <summary>A run happens at start-up and then every hour, each sweeping at most <c>MaxAccountsPerRun</c> accounts.</summary>
    [Fact]
    public async Task ItSweeps_AtStartUp_AndEveryHour()
    {
        _files.OnRemoveStaleUploadsAsync = (_, _) => Task.FromResult(0);
        using FileManagerUploadSweepScheduler scheduler = Sut();

        await scheduler.StartAsync(TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromMinutes(59));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await CalledAsync(2);
        await scheduler.StopAsync(TestContext.Current.CancellationToken);

        Assert.All(_files.RemoveStaleUploadsAsyncCalls, call => Assert.Equal(FileManagerUploadSweepScheduler.MaxAccountsPerRun, call.MaxAccounts));
    }

    /// <summary>A run that throws is logged as an error with its exception, and the next hour's run still happens.</summary>
    [Fact]
    public async Task AFailingRun_IsLogged_AndTheNextRunStillHappens()
    {
        var broken = new InvalidOperationException("no database");
        var runs = 0;
        _files.OnRemoveStaleUploadsAsync = (_, _) => ++runs == 1 ? Task.FromException<int>(broken) : Task.FromResult(0);
        using FileManagerUploadSweepScheduler scheduler = Sut();

        await scheduler.StartAsync(TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromHours(1));
        await CalledAsync(2);
        await scheduler.StopAsync(TestContext.Current.CancellationToken);

        FakeLogRecord log = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal("Removing the File Manager's abandoned uploads failed; trying again in an hour", log.Message);
        Assert.Same(broken, log.Exception);
    }
}
