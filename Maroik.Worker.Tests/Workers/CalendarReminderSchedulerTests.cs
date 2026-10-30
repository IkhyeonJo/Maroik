using Maroik.Core.Contract.Interfaces;
using Maroik.Worker.Tests.Fixtures;
using Maroik.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// Unit tests for <see cref="CalendarReminderScheduler"/>: it sends the due calendar reminders when it starts and then once a minute
/// (on the injected clock), each run in a scope of its own and bounded to <see cref="CalendarReminderScheduler.MaxPerRun"/>; a
/// failing run is logged and the next minute's run still happens.
/// </summary>
public class CalendarReminderSchedulerTests
{
    private readonly FakeCalendarReminderDispatchService _dispatch = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero));
    private readonly FakeLogger<CalendarReminderScheduler> _logger = new();

    /// <summary>The scheduler over a container holding the fake dispatch service.</summary>
    private CalendarReminderScheduler Sut()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalendarReminderDispatchService>(_ => _dispatch);
        return new CalendarReminderScheduler(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _clock, _logger);
    }

    /// <summary>Waits (real time, bounded) until the dispatch service has been called <paramref name="times"/> times.</summary>
    private async Task CalledAsync(int times)
    {
        for (int i = 0; i < 200 && _dispatch.DispatchDueAsyncCalls.Count < times; i++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(times, _dispatch.DispatchDueAsyncCalls.Count);
    }

    /// <summary>A run happens at start-up and then every minute, each asking for at most <c>MaxPerRun</c> reminders.</summary>
    [Fact]
    public async Task ItSends_AtStartUp_AndEveryMinute()
    {
        _dispatch.OnDispatchDueAsync = (_, _) => Task.FromResult(0);
        using CalendarReminderScheduler scheduler = Sut();

        await scheduler.StartAsync(TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromSeconds(59));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await CalledAsync(2);
        await scheduler.StopAsync(TestContext.Current.CancellationToken);

        Assert.All(_dispatch.DispatchDueAsyncCalls, call => Assert.Equal(CalendarReminderScheduler.MaxPerRun, call.Max));
    }

    /// <summary>A run that throws is logged as an error with its exception, and the next minute's run still happens.</summary>
    [Fact]
    public async Task AFailingRun_IsLogged_AndTheNextRunStillHappens()
    {
        var broken = new InvalidOperationException("no database");
        var runs = 0;
        _dispatch.OnDispatchDueAsync = (_, _) => ++runs == 1 ? Task.FromException<int>(broken) : Task.FromResult(0);
        using CalendarReminderScheduler scheduler = Sut();

        await scheduler.StartAsync(TestContext.Current.CancellationToken);
        await CalledAsync(1);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await CalledAsync(2);
        await scheduler.StopAsync(TestContext.Current.CancellationToken);

        FakeLogRecord log = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal("Sending the due calendar reminders failed; trying again in a minute", log.Message);
        Assert.Same(broken, log.Exception);
    }
}
