using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarRepository : ICalendarRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<Calendar>>>? OnGetByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<Calendar>>> Answer)> _getByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>GetByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, Task<List<Calendar>>> answer) => AnswerGetByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<Calendar>>> answer)
    {
        lock (_getByAccountEmailAsyncRules) _getByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Calendar>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Add((accountEmail, ct));
        Func<string, CancellationToken, Task<List<Calendar>>>? answer = null;
        lock (_getByAccountEmailAsyncRules) answer = _getByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, ct)).Answer;
        if ((answer ?? OnGetByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<Calendar>>([]);
    }

    /// <summary>What <c>FindByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Calendar?>>? OnFindByIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Calendar?>> Answer)> _findByIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, Task<Calendar?>> answer) => AnswerFindByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Calendar?>> answer)
    {
        lock (_findByIdAsyncRules) _findByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Calendar?> FindByIdAsync(long id, CancellationToken ct)
    {
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Calendar?>>? answer = null;
        lock (_findByIdAsyncRules) answer = _findByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdAsync was called without an answer on a strict fake.") : Task.FromResult<Calendar?>(default);
    }

    /// <summary>What <c>FindByIdForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Calendar?>>? OnFindByIdForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Calendar?>> Answer)> _findByIdForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, Task<Calendar?>> answer) => AnswerFindByIdForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Calendar?>> answer)
    {
        lock (_findByIdForUpdateAsyncRules) _findByIdForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Calendar?> FindByIdForUpdateAsync(long id, CancellationToken ct)
    {
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Calendar?>>? answer = null;
        lock (_findByIdForUpdateAsyncRules) answer = _findByIdForUpdateAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdForUpdateAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<Calendar?>(default);
    }

    /// <summary>What <c>GetSharedWithGuestsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Calendar>>>? OnGetSharedWithGuestsAsync { get; set; }

    /// <summary>The arguments of every <c>GetSharedWithGuestsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetSharedWithGuestsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetSharedWithGuestsAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Calendar>>> Answer)> _getSharedWithGuestsAsyncRules = [];

    /// <summary>Makes every later <c>GetSharedWithGuestsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSharedWithGuestsAsync(Func<CancellationToken, Task<List<Calendar>>> answer) => AnswerGetSharedWithGuestsAsync((_) => true, answer);

    /// <summary>Makes a later <c>GetSharedWithGuestsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSharedWithGuestsAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Calendar>>> answer)
    {
        lock (_getSharedWithGuestsAsyncRules) _getSharedWithGuestsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Calendar>> GetSharedWithGuestsAsync(CancellationToken ct)
    {
        lock (GetSharedWithGuestsAsyncCalls) GetSharedWithGuestsAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Calendar>>>? answer = null;
        lock (_getSharedWithGuestsAsyncRules) answer = _getSharedWithGuestsAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnGetSharedWithGuestsAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("GetSharedWithGuestsAsync was called without an answer on a strict fake.") : Task.FromResult<List<Calendar>>([]);
    }

    /// <summary>What <c>GetSharedWithUsersAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Calendar>>>? OnGetSharedWithUsersAsync { get; set; }

    /// <summary>The arguments of every <c>GetSharedWithUsersAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetSharedWithUsersAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetSharedWithUsersAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Calendar>>> Answer)> _getSharedWithUsersAsyncRules = [];

    /// <summary>Makes every later <c>GetSharedWithUsersAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSharedWithUsersAsync(Func<CancellationToken, Task<List<Calendar>>> answer) => AnswerGetSharedWithUsersAsync((_) => true, answer);

    /// <summary>Makes a later <c>GetSharedWithUsersAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSharedWithUsersAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Calendar>>> answer)
    {
        lock (_getSharedWithUsersAsyncRules) _getSharedWithUsersAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Calendar>> GetSharedWithUsersAsync(CancellationToken ct)
    {
        lock (GetSharedWithUsersAsyncCalls) GetSharedWithUsersAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Calendar>>>? answer = null;
        lock (_getSharedWithUsersAsyncRules) answer = _getSharedWithUsersAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnGetSharedWithUsersAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("GetSharedWithUsersAsync was called without an answer on a strict fake.") : Task.FromResult<List<Calendar>>([]);
    }

    /// <summary>What <c>GetSubscribedByAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<Calendar>>>? OnGetSubscribedByAsync { get; set; }

    /// <summary>The arguments of every <c>GetSubscribedByAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetSubscribedByAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetSubscribedByAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<Calendar>>> Answer)> _getSubscribedByAsyncRules = [];

    /// <summary>Makes every later <c>GetSubscribedByAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSubscribedByAsync(Func<string, CancellationToken, Task<List<Calendar>>> answer) => AnswerGetSubscribedByAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetSubscribedByAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetSubscribedByAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<Calendar>>> answer)
    {
        lock (_getSubscribedByAsyncRules) _getSubscribedByAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Calendar>> GetSubscribedByAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetSubscribedByAsyncCalls) GetSubscribedByAsyncCalls.Add((accountEmail, ct));
        Func<string, CancellationToken, Task<List<Calendar>>>? answer = null;
        lock (_getSubscribedByAsyncRules) answer = _getSubscribedByAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, ct)).Answer;
        if ((answer ?? OnGetSubscribedByAsync) is { } on)
        {
            return on(accountEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("GetSubscribedByAsync was called without an answer on a strict fake.") : Task.FromResult<List<Calendar>>([]);
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
    public Func<Calendar?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Calendar? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<Calendar?, CancellationToken, bool> Match, Func<Calendar?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Calendar?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Calendar?, CancellationToken, bool> match, Func<Calendar?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(Calendar? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<Calendar?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Calendar?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Calendar? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<Calendar?, CancellationToken, bool> Match, Func<Calendar?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Calendar?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Calendar?, CancellationToken, bool> match, Func<Calendar?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(Calendar? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<Calendar?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByAccountEmailAsyncCalls.Count + FindByIdAsyncCalls.Count + FindByIdForUpdateAsyncCalls.Count + GetSharedWithGuestsAsyncCalls.Count + GetSharedWithUsersAsyncCalls.Count + GetSubscribedByAsyncCalls.Count + DeleteByIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Clear();
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Clear();
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Clear();
        lock (GetSharedWithGuestsAsyncCalls) GetSharedWithGuestsAsyncCalls.Clear();
        lock (GetSharedWithUsersAsyncCalls) GetSharedWithUsersAsyncCalls.Clear();
        lock (GetSubscribedByAsyncCalls) GetSubscribedByAsyncCalls.Clear();
        lock (DeleteByIdAsyncCalls) DeleteByIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
