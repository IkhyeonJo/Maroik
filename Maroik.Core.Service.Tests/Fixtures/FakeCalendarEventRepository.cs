using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarEventRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarEventRepository : ICalendarEventRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>CreateCalendarEventAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEvent, CancellationToken, Task<long>>? OnCreateCalendarEventAsync { get; set; }

    /// <summary>The arguments of every <c>CreateCalendarEventAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEvent CalendarEvent, CancellationToken Ct)> CreateCalendarEventAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateCalendarEventAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEvent, CancellationToken, bool> Match, Func<CalendarEvent, CancellationToken, Task<long>> Answer)> _createCalendarEventAsyncRules = [];

    /// <summary>Makes every later <c>CreateCalendarEventAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateCalendarEventAsync(Func<CalendarEvent, CancellationToken, Task<long>> answer) => AnswerCreateCalendarEventAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateCalendarEventAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateCalendarEventAsync(Func<CalendarEvent, CancellationToken, bool> match, Func<CalendarEvent, CancellationToken, Task<long>> answer)
    {
        lock (_createCalendarEventAsyncRules) _createCalendarEventAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken ct)
    {
        lock (CreateCalendarEventAsyncCalls) CreateCalendarEventAsyncCalls.Add((calendarEvent, ct));
        Func<CalendarEvent, CancellationToken, Task<long>>? answer = null;
        lock (_createCalendarEventAsyncRules) answer = _createCalendarEventAsyncRules.LastOrDefault(rule => rule.Match(calendarEvent, ct)).Answer;
        if ((answer ?? OnCreateCalendarEventAsync) is { } on)
        {
            return on(calendarEvent, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateCalendarEventAsync was called without an answer on a strict fake.") : Task.FromResult<long>(default);
    }

    /// <summary>What <c>GetByCalendarIdsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<List<CalendarEvent>>>? OnGetByCalendarIdsAsync { get; set; }

    /// <summary>The arguments of every <c>GetByCalendarIdsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> CalendarIds, CancellationToken Ct)> GetByCalendarIdsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByCalendarIdsAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<long>, CancellationToken, bool> Match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarEvent>>> Answer)> _getByCalendarIdsAsyncRules = [];

    /// <summary>Makes every later <c>GetByCalendarIdsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByCalendarIdsAsync(Func<IEnumerable<long>, CancellationToken, Task<List<CalendarEvent>>> answer) => AnswerGetByCalendarIdsAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByCalendarIdsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByCalendarIdsAsync(Func<IEnumerable<long>, CancellationToken, bool> match, Func<IEnumerable<long>, CancellationToken, Task<List<CalendarEvent>>> answer)
    {
        lock (_getByCalendarIdsAsyncRules) _getByCalendarIdsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<CalendarEvent>> GetByCalendarIdsAsync(IEnumerable<long> calendarIds, CancellationToken ct)
    {
        lock (GetByCalendarIdsAsyncCalls) GetByCalendarIdsAsyncCalls.Add((calendarIds, ct));
        Func<IEnumerable<long>, CancellationToken, Task<List<CalendarEvent>>>? answer = null;
        lock (_getByCalendarIdsAsyncRules) answer = _getByCalendarIdsAsyncRules.LastOrDefault(rule => rule.Match(calendarIds, ct)).Answer;
        if ((answer ?? OnGetByCalendarIdsAsync) is { } on)
        {
            return on(calendarIds, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByCalendarIdsAsync was called without an answer on a strict fake.") : Task.FromResult<List<CalendarEvent>>([]);
    }

    /// <summary>What <c>FindByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<CalendarEvent?>>? OnFindByIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<CalendarEvent?>> Answer)> _findByIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, Task<CalendarEvent?>> answer) => AnswerFindByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<CalendarEvent?>> answer)
    {
        lock (_findByIdAsyncRules) _findByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<CalendarEvent?> FindByIdAsync(long id, CancellationToken ct)
    {
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<CalendarEvent?>>? answer = null;
        lock (_findByIdAsyncRules) answer = _findByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdAsync was called without an answer on a strict fake.") : Task.FromResult<CalendarEvent?>(default);
    }

    /// <summary>What <c>FindByIdForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<CalendarEvent?>>? OnFindByIdForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<CalendarEvent?>> Answer)> _findByIdForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, Task<CalendarEvent?>> answer) => AnswerFindByIdForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<CalendarEvent?>> answer)
    {
        lock (_findByIdForUpdateAsyncRules) _findByIdForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<CalendarEvent?> FindByIdForUpdateAsync(long id, CancellationToken ct)
    {
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<CalendarEvent?>>? answer = null;
        lock (_findByIdForUpdateAsyncRules) answer = _findByIdForUpdateAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdForUpdateAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<CalendarEvent?>(default);
    }

    /// <summary>What <c>DeleteByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task>? OnDeleteByIdAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> DeleteByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task> Answer)> _deleteByIdAsyncRules = [];

    /// <summary>Makes every later <c>DeleteByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByIdAsync(Func<long, CancellationToken, Task> answer) => AnswerDeleteByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>DeleteByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task> answer)
    {
        lock (_deleteByIdAsyncRules) _deleteByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task DeleteByIdAsync(long id, CancellationToken ct)
    {
        lock (DeleteByIdAsyncCalls) DeleteByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task>? answer = null;
        lock (_deleteByIdAsyncRules) answer = _deleteByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnDeleteByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteByIdAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEvent?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEvent? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEvent?, CancellationToken, bool> Match, Func<CalendarEvent?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEvent?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<CalendarEvent?, CancellationToken, bool> match, Func<CalendarEvent?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(CalendarEvent? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<CalendarEvent?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEvent?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEvent? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<CalendarEvent?, CancellationToken, bool> Match, Func<CalendarEvent?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEvent?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<CalendarEvent?, CancellationToken, bool> match, Func<CalendarEvent?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(CalendarEvent? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<CalendarEvent?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => CreateCalendarEventAsyncCalls.Count + GetByCalendarIdsAsyncCalls.Count + FindByIdAsyncCalls.Count + FindByIdForUpdateAsyncCalls.Count + DeleteByIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (CreateCalendarEventAsyncCalls) CreateCalendarEventAsyncCalls.Clear();
        lock (GetByCalendarIdsAsyncCalls) GetByCalendarIdsAsyncCalls.Clear();
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Clear();
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Clear();
        lock (DeleteByIdAsyncCalls) DeleteByIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
