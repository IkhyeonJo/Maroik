using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarSharedRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarSharedRepository : ICalendarSharedRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByIdsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>>? OnGetByIdsAsync { get; set; }

    /// <summary>The arguments of every <c>GetByIdsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> CalendarIds, CancellationToken Ct)> GetByIdsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByIdsAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<long>, CancellationToken, bool> Match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> Answer)> _getByIdsAsyncRules = [];

    /// <summary>Makes every later <c>GetByIdsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByIdsAsync(Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> answer) => AnswerGetByIdsAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByIdsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByIdsAsync(Func<IEnumerable<long>, CancellationToken, bool> match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> answer)
    {
        lock (_getByIdsAsyncRules) _getByIdsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<CalendarShared>> GetByIdsAsync(IEnumerable<long> calendarIds, CancellationToken ct)
    {
        lock (GetByIdsAsyncCalls) GetByIdsAsyncCalls.Add((calendarIds, ct));
        Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>>? answer = null;
        lock (_getByIdsAsyncRules) answer = _getByIdsAsyncRules.LastOrDefault(rule => rule.Match(calendarIds, ct)).Answer;
        if ((answer ?? OnGetByIdsAsync) is { } on)
        {
            return on(calendarIds, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByIdsAsync was called without an answer on a strict fake.") : Task.FromResult<List<CalendarShared>>([]);
    }

    /// <summary>What <c>GetByIdsForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>>? OnGetByIdsForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>GetByIdsForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> CalendarIds, CancellationToken Ct)> GetByIdsForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByIdsForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<long>, CancellationToken, bool> Match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> Answer)> _getByIdsForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>GetByIdsForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByIdsForUpdateAsync(Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> answer) => AnswerGetByIdsForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByIdsForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByIdsForUpdateAsync(Func<IEnumerable<long>, CancellationToken, bool> match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>> answer)
    {
        lock (_getByIdsForUpdateAsyncRules) _getByIdsForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<CalendarShared>> GetByIdsForUpdateAsync(IEnumerable<long> calendarIds, CancellationToken ct)
    {
        lock (GetByIdsForUpdateAsyncCalls) GetByIdsForUpdateAsyncCalls.Add((calendarIds, ct));
        Func<IEnumerable<long>, CancellationToken, Task<List<CalendarShared>>>? answer = null;
        lock (_getByIdsForUpdateAsyncRules) answer = _getByIdsForUpdateAsyncRules.LastOrDefault(rule => rule.Match(calendarIds, ct)).Answer;
        if ((answer ?? OnGetByIdsForUpdateAsync) is { } on)
        {
            return on(calendarIds, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByIdsForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<List<CalendarShared>>([]);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarShared?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarShared? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarShared?, CancellationToken, bool> Match, Func<CalendarShared?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarShared?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarShared?, CancellationToken, bool> match, Func<CalendarShared?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(CalendarShared? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<CalendarShared?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarShared?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarShared? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarShared?, CancellationToken, bool> Match, Func<CalendarShared?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarShared?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarShared?, CancellationToken, bool> match, Func<CalendarShared?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(CalendarShared? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<CalendarShared?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByIdsAsyncCalls.Count + GetByIdsForUpdateAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByIdsAsyncCalls) GetByIdsAsyncCalls.Clear();
        lock (GetByIdsForUpdateAsyncCalls) GetByIdsForUpdateAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
