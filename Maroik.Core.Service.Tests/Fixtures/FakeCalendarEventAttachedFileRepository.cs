using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarEventAttachedFileRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarEventAttachedFileRepository : ICalendarEventAttachedFileRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>FindByCalendarEventIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<CalendarEventAttachedFile?>>? OnFindByCalendarEventIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByCalendarEventIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, CancellationToken Ct)> FindByCalendarEventIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByCalendarEventIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<CalendarEventAttachedFile?>> Answer)> _findByCalendarEventIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByCalendarEventIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByCalendarEventIdAsync(Func<long, CancellationToken, Task<CalendarEventAttachedFile?>> answer) => AnswerFindByCalendarEventIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByCalendarEventIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByCalendarEventIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<CalendarEventAttachedFile?>> answer)
    {
        lock (_findByCalendarEventIdAsyncRules) _findByCalendarEventIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<CalendarEventAttachedFile?> FindByCalendarEventIdAsync(long calendarEventId, CancellationToken ct)
    {
        lock (FindByCalendarEventIdAsyncCalls) FindByCalendarEventIdAsyncCalls.Add((calendarEventId, ct));
        Func<long, CancellationToken, Task<CalendarEventAttachedFile?>>? answer = null;
        lock (_findByCalendarEventIdAsyncRules) answer = _findByCalendarEventIdAsyncRules.LastOrDefault(rule => rule.Match(calendarEventId, ct)).Answer;
        if ((answer ?? OnFindByCalendarEventIdAsync) is { } on)
        {
            return on(calendarEventId, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByCalendarEventIdAsync was called without an answer on a strict fake.") : Task.FromResult<CalendarEventAttachedFile?>(default);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventAttachedFile?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventAttachedFile? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEventAttachedFile?, CancellationToken, bool> Match, Func<CalendarEventAttachedFile?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEventAttachedFile?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEventAttachedFile?, CancellationToken, bool> match, Func<CalendarEventAttachedFile?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(CalendarEventAttachedFile? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<CalendarEventAttachedFile?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventAttachedFile?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventAttachedFile? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEventAttachedFile?, CancellationToken, bool> Match, Func<CalendarEventAttachedFile?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEventAttachedFile?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEventAttachedFile?, CancellationToken, bool> match, Func<CalendarEventAttachedFile?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(CalendarEventAttachedFile? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<CalendarEventAttachedFile?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => FindByCalendarEventIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (FindByCalendarEventIdAsyncCalls) FindByCalendarEventIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
