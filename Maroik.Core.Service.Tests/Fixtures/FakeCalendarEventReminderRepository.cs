using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarEventReminderRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarEventReminderRepository : ICalendarEventReminderRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByCalendarEventIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<List<CalendarEventReminder>>>? OnGetByCalendarEventIdAsync { get; set; }

    /// <summary>The arguments of every <c>GetByCalendarEventIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, CancellationToken Ct)> GetByCalendarEventIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByCalendarEventIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<List<CalendarEventReminder>>> Answer)> _getByCalendarEventIdAsyncRules = [];

    /// <summary>Makes every later <c>GetByCalendarEventIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByCalendarEventIdAsync(Func<long, CancellationToken, Task<List<CalendarEventReminder>>> answer) => AnswerGetByCalendarEventIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByCalendarEventIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByCalendarEventIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<List<CalendarEventReminder>>> answer)
    {
        lock (_getByCalendarEventIdAsyncRules) _getByCalendarEventIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<CalendarEventReminder>> GetByCalendarEventIdAsync(long calendarEventId, CancellationToken ct)
    {
        lock (GetByCalendarEventIdAsyncCalls) GetByCalendarEventIdAsyncCalls.Add((calendarEventId, ct));
        Func<long, CancellationToken, Task<List<CalendarEventReminder>>>? answer = null;
        lock (_getByCalendarEventIdAsyncRules) answer = _getByCalendarEventIdAsyncRules.LastOrDefault(rule => rule.Match(calendarEventId, ct)).Answer;
        if ((answer ?? OnGetByCalendarEventIdAsync) is { } on)
        {
            return on(calendarEventId, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByCalendarEventIdAsync was called without an answer on a strict fake.") : Task.FromResult<List<CalendarEventReminder>>([]);
    }

    /// <summary>What <c>LockDueAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<DateTime, int, CancellationToken, Task<List<CalendarEventReminder>>>? OnLockDueAsync { get; set; }

    /// <summary>The arguments of every <c>LockDueAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(DateTime NowUtc, int Limit, CancellationToken Ct)> LockDueAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerLockDueAsync</c> added, newest last.</summary>
    private readonly List<(Func<DateTime, int, CancellationToken, bool> Match, Func<DateTime, int, CancellationToken, Task<List<CalendarEventReminder>>> Answer)> _lockDueAsyncRules = [];

    /// <summary>Makes every later <c>LockDueAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerLockDueAsync(Func<DateTime, int, CancellationToken, Task<List<CalendarEventReminder>>> answer) => AnswerLockDueAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>LockDueAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerLockDueAsync(Func<DateTime, int, CancellationToken, bool> match, Func<DateTime, int, CancellationToken, Task<List<CalendarEventReminder>>> answer)
    {
        lock (_lockDueAsyncRules) _lockDueAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<CalendarEventReminder>> LockDueAsync(DateTime nowUtc, int limit, CancellationToken ct)
    {
        lock (LockDueAsyncCalls) LockDueAsyncCalls.Add((nowUtc, limit, ct));
        Func<DateTime, int, CancellationToken, Task<List<CalendarEventReminder>>>? answer = null;
        lock (_lockDueAsyncRules) answer = _lockDueAsyncRules.LastOrDefault(rule => rule.Match(nowUtc, limit, ct)).Answer;
        if ((answer ?? OnLockDueAsync) is { } on)
        {
            return on(nowUtc, limit, ct);
        }

        return Strict ? throw new InvalidOperationException("LockDueAsync was called without an answer on a strict fake.") : Task.FromResult<List<CalendarEventReminder>>([]);
    }

    /// <summary>What <c>DeleteByCalendarEventIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task>? OnDeleteByCalendarEventIdAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteByCalendarEventIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, CancellationToken Ct)> DeleteByCalendarEventIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteByCalendarEventIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task> Answer)> _deleteByCalendarEventIdAsyncRules = [];

    /// <summary>Makes every later <c>DeleteByCalendarEventIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByCalendarEventIdAsync(Func<long, CancellationToken, Task> answer) => AnswerDeleteByCalendarEventIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>DeleteByCalendarEventIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByCalendarEventIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task> answer)
    {
        lock (_deleteByCalendarEventIdAsyncRules) _deleteByCalendarEventIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task DeleteByCalendarEventIdAsync(long calendarEventId, CancellationToken ct)
    {
        lock (DeleteByCalendarEventIdAsyncCalls) DeleteByCalendarEventIdAsyncCalls.Add((calendarEventId, ct));
        Func<long, CancellationToken, Task>? answer = null;
        lock (_deleteByCalendarEventIdAsyncRules) answer = _deleteByCalendarEventIdAsyncRules.LastOrDefault(rule => rule.Match(calendarEventId, ct)).Answer;
        if ((answer ?? OnDeleteByCalendarEventIdAsync) is { } on)
        {
            return on(calendarEventId, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteByCalendarEventIdAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventReminder?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventReminder? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEventReminder?, CancellationToken, bool> Match, Func<CalendarEventReminder?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEventReminder?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEventReminder?, CancellationToken, bool> match, Func<CalendarEventReminder?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(CalendarEventReminder? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<CalendarEventReminder?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventReminder?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventReminder? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEventReminder?, CancellationToken, bool> Match, Func<CalendarEventReminder?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEventReminder?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEventReminder?, CancellationToken, bool> match, Func<CalendarEventReminder?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(CalendarEventReminder? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<CalendarEventReminder?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByCalendarEventIdAsyncCalls.Count + LockDueAsyncCalls.Count + DeleteByCalendarEventIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByCalendarEventIdAsyncCalls) GetByCalendarEventIdAsyncCalls.Clear();
        lock (LockDueAsyncCalls) LockDueAsyncCalls.Clear();
        lock (DeleteByCalendarEventIdAsyncCalls) DeleteByCalendarEventIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
