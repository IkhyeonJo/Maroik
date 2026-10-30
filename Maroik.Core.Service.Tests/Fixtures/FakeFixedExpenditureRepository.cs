using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IFixedExpenditureRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeFixedExpenditureRepository : IFixedExpenditureRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<FixedExpenditure>>>? OnGetByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<FixedExpenditure>>> Answer)> _getByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>GetByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, Task<List<FixedExpenditure>>> answer) => AnswerGetByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<FixedExpenditure>>> answer)
    {
        lock (_getByAccountEmailAsyncRules) _getByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<FixedExpenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Add((accountEmail, ct));
        Func<string, CancellationToken, Task<List<FixedExpenditure>>>? answer = null;
        lock (_getByAccountEmailAsyncRules) answer = _getByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, ct)).Answer;
        if ((answer ?? OnGetByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<FixedExpenditure>>([]);
    }

    /// <summary>What <c>SearchByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<List<FixedExpenditure>>>? OnSearchByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>SearchByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string Search, CancellationToken Ct)> SearchByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerSearchByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<List<FixedExpenditure>>> Answer)> _searchByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>SearchByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchByAccountEmailAsync(Func<string, string, CancellationToken, Task<List<FixedExpenditure>>> answer) => AnswerSearchByAccountEmailAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>SearchByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchByAccountEmailAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<List<FixedExpenditure>>> answer)
    {
        lock (_searchByAccountEmailAsyncRules) _searchByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<FixedExpenditure>> SearchByAccountEmailAsync(string accountEmail, string search, CancellationToken ct)
    {
        lock (SearchByAccountEmailAsyncCalls) SearchByAccountEmailAsyncCalls.Add((accountEmail, search, ct));
        Func<string, string, CancellationToken, Task<List<FixedExpenditure>>>? answer = null;
        lock (_searchByAccountEmailAsyncRules) answer = _searchByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, search, ct)).Answer;
        if ((answer ?? OnSearchByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, search, ct);
        }

        return Strict ? throw new InvalidOperationException("SearchByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<FixedExpenditure>>([]);
    }

    /// <summary>What <c>FindByEmailAndIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task<FixedExpenditure?>>? OnFindByEmailAndIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAndIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, CancellationToken Ct)> FindByEmailAndIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAndIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, long, CancellationToken, bool> Match, Func<string, long, CancellationToken, Task<FixedExpenditure?>> Answer)> _findByEmailAndIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAndIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndIdAsync(Func<string, long, CancellationToken, Task<FixedExpenditure?>> answer) => AnswerFindByEmailAndIdAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAndIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndIdAsync(Func<string, long, CancellationToken, bool> match, Func<string, long, CancellationToken, Task<FixedExpenditure?>> answer)
    {
        lock (_findByEmailAndIdAsyncRules) _findByEmailAndIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<FixedExpenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct)
    {
        lock (FindByEmailAndIdAsyncCalls) FindByEmailAndIdAsyncCalls.Add((accountEmail, id, ct));
        Func<string, long, CancellationToken, Task<FixedExpenditure?>>? answer = null;
        lock (_findByEmailAndIdAsyncRules) answer = _findByEmailAndIdAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, id, ct)).Answer;
        if ((answer ?? OnFindByEmailAndIdAsync) is { } on)
        {
            return on(accountEmail, id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAndIdAsync was called without an answer on a strict fake.") : Task.FromResult<FixedExpenditure?>(default);
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
    public Func<FixedExpenditure?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(FixedExpenditure? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<FixedExpenditure?, CancellationToken, bool> Match, Func<FixedExpenditure?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<FixedExpenditure?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<FixedExpenditure?, CancellationToken, bool> match, Func<FixedExpenditure?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(FixedExpenditure? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<FixedExpenditure?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<FixedExpenditure?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(FixedExpenditure? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<FixedExpenditure?, CancellationToken, bool> Match, Func<FixedExpenditure?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<FixedExpenditure?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<FixedExpenditure?, CancellationToken, bool> match, Func<FixedExpenditure?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(FixedExpenditure? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<FixedExpenditure?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByAccountEmailAsyncCalls.Count + SearchByAccountEmailAsyncCalls.Count + FindByEmailAndIdAsyncCalls.Count + DeleteByIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Clear();
        lock (SearchByAccountEmailAsyncCalls) SearchByAccountEmailAsyncCalls.Clear();
        lock (FindByEmailAndIdAsyncCalls) FindByEmailAndIdAsyncCalls.Clear();
        lock (DeleteByIdAsyncCalls) DeleteByIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
