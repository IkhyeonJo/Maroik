using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IOtherCalendarRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeOtherCalendarRepository : IOtherCalendarRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<OtherCalendar>>>? OnGetByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> GetByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<OtherCalendar>>> Answer)> _getByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>GetByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, Task<List<OtherCalendar>>> answer) => AnswerGetByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<OtherCalendar>>> answer)
    {
        lock (_getByAccountEmailAsyncRules) _getByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<OtherCalendar>> GetByAccountEmailAsync(string email, CancellationToken ct)
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Add((email, ct));
        Func<string, CancellationToken, Task<List<OtherCalendar>>>? answer = null;
        lock (_getByAccountEmailAsyncRules) answer = _getByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(email, ct)).Answer;
        if ((answer ?? OnGetByAccountEmailAsync) is { } on)
        {
            return on(email, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<OtherCalendar>>([]);
    }

    /// <summary>What <c>DeleteByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnDeleteByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> DeleteByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task> Answer)> _deleteByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>DeleteByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByAccountEmailAsync(Func<string, CancellationToken, Task> answer) => AnswerDeleteByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>DeleteByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task> answer)
    {
        lock (_deleteByAccountEmailAsyncRules) _deleteByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task DeleteByAccountEmailAsync(string email, CancellationToken ct)
    {
        lock (DeleteByAccountEmailAsyncCalls) DeleteByAccountEmailAsyncCalls.Add((email, ct));
        Func<string, CancellationToken, Task>? answer = null;
        lock (_deleteByAccountEmailAsyncRules) answer = _deleteByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(email, ct)).Answer;
        if ((answer ?? OnDeleteByAccountEmailAsync) is { } on)
        {
            return on(email, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteByAccountEmailAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>DeleteByCalendarIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task>? OnDeleteByCalendarIdAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteByCalendarIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarId, CancellationToken Ct)> DeleteByCalendarIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteByCalendarIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task> Answer)> _deleteByCalendarIdAsyncRules = [];

    /// <summary>Makes every later <c>DeleteByCalendarIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByCalendarIdAsync(Func<long, CancellationToken, Task> answer) => AnswerDeleteByCalendarIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>DeleteByCalendarIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByCalendarIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task> answer)
    {
        lock (_deleteByCalendarIdAsyncRules) _deleteByCalendarIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task DeleteByCalendarIdAsync(long calendarId, CancellationToken ct)
    {
        lock (DeleteByCalendarIdAsyncCalls) DeleteByCalendarIdAsyncCalls.Add((calendarId, ct));
        Func<long, CancellationToken, Task>? answer = null;
        lock (_deleteByCalendarIdAsyncRules) answer = _deleteByCalendarIdAsyncRules.LastOrDefault(rule => rule.Match(calendarId, ct)).Answer;
        if ((answer ?? OnDeleteByCalendarIdAsync) is { } on)
        {
            return on(calendarId, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteByCalendarIdAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<OtherCalendar?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(OtherCalendar? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<OtherCalendar?, CancellationToken, bool> Match, Func<OtherCalendar?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<OtherCalendar?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<OtherCalendar?, CancellationToken, bool> match, Func<OtherCalendar?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(OtherCalendar? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<OtherCalendar?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<OtherCalendar?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(OtherCalendar? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<OtherCalendar?, CancellationToken, bool> Match, Func<OtherCalendar?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<OtherCalendar?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<OtherCalendar?, CancellationToken, bool> match, Func<OtherCalendar?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(OtherCalendar? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<OtherCalendar?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByAccountEmailAsyncCalls.Count + DeleteByAccountEmailAsyncCalls.Count + DeleteByCalendarIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Clear();
        lock (DeleteByAccountEmailAsyncCalls) DeleteByAccountEmailAsyncCalls.Clear();
        lock (DeleteByCalendarIdAsyncCalls) DeleteByCalendarIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
