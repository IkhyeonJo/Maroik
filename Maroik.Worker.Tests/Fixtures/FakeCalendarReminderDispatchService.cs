using Maroik.Core.Contract.Interfaces;

namespace Maroik.Worker.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarReminderDispatchService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarReminderDispatchService : ICalendarReminderDispatchService
{
    /// <summary>What <c>DispatchDueAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<int, CancellationToken, Task<int>>? OnDispatchDueAsync { get; set; }

    /// <summary>The arguments of every <c>DispatchDueAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(int Max, CancellationToken Ct)> DispatchDueAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<int> DispatchDueAsync(int max, CancellationToken ct)
    {
        lock (DispatchDueAsyncCalls) DispatchDueAsyncCalls.Add((max, ct));
        if (OnDispatchDueAsync is { } on)
        {
            return on(max, ct);
        }

        return Task.FromResult<int>(default);
    }
}
