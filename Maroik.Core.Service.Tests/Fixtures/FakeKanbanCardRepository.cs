using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Kanbans;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IKanbanCardRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeKanbanCardRepository : IKanbanCardRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<KanbanCard>>>? OnGetByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<KanbanCard>>> Answer)> _getByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>GetByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, Task<List<KanbanCard>>> answer) => AnswerGetByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<KanbanCard>>> answer)
    {
        lock (_getByAccountEmailAsyncRules) _getByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<KanbanCard>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Add((accountEmail, ct));
        Func<string, CancellationToken, Task<List<KanbanCard>>>? answer = null;
        lock (_getByAccountEmailAsyncRules) answer = _getByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, ct)).Answer;
        if ((answer ?? OnGetByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<KanbanCard>>([]);
    }

    /// <summary>What <c>FindByEmailAndIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task<KanbanCard?>>? OnFindByEmailAndIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAndIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, CancellationToken Ct)> FindByEmailAndIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAndIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, long, CancellationToken, bool> Match, Func<string, long, CancellationToken, Task<KanbanCard?>> Answer)> _findByEmailAndIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAndIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndIdAsync(Func<string, long, CancellationToken, Task<KanbanCard?>> answer) => AnswerFindByEmailAndIdAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAndIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndIdAsync(Func<string, long, CancellationToken, bool> match, Func<string, long, CancellationToken, Task<KanbanCard?>> answer)
    {
        lock (_findByEmailAndIdAsyncRules) _findByEmailAndIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<KanbanCard?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct)
    {
        lock (FindByEmailAndIdAsyncCalls) FindByEmailAndIdAsyncCalls.Add((accountEmail, id, ct));
        Func<string, long, CancellationToken, Task<KanbanCard?>>? answer = null;
        lock (_findByEmailAndIdAsyncRules) answer = _findByEmailAndIdAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, id, ct)).Answer;
        if ((answer ?? OnFindByEmailAndIdAsync) is { } on)
        {
            return on(accountEmail, id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAndIdAsync was called without an answer on a strict fake.") : Task.FromResult<KanbanCard?>(default);
    }

    /// <summary>What <c>DeleteByEmailAndIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task>? OnDeleteByEmailAndIdAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteByEmailAndIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, CancellationToken Ct)> DeleteByEmailAndIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteByEmailAndIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, long, CancellationToken, bool> Match, Func<string, long, CancellationToken, Task> Answer)> _deleteByEmailAndIdAsyncRules = [];

    /// <summary>Makes every later <c>DeleteByEmailAndIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByEmailAndIdAsync(Func<string, long, CancellationToken, Task> answer) => AnswerDeleteByEmailAndIdAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>DeleteByEmailAndIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteByEmailAndIdAsync(Func<string, long, CancellationToken, bool> match, Func<string, long, CancellationToken, Task> answer)
    {
        lock (_deleteByEmailAndIdAsyncRules) _deleteByEmailAndIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task DeleteByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct)
    {
        lock (DeleteByEmailAndIdAsyncCalls) DeleteByEmailAndIdAsyncCalls.Add((accountEmail, id, ct));
        Func<string, long, CancellationToken, Task>? answer = null;
        lock (_deleteByEmailAndIdAsyncRules) answer = _deleteByEmailAndIdAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, id, ct)).Answer;
        if ((answer ?? OnDeleteByEmailAndIdAsync) is { } on)
        {
            return on(accountEmail, id, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteByEmailAndIdAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<KanbanCard?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(KanbanCard? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<KanbanCard?, CancellationToken, bool> Match, Func<KanbanCard?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<KanbanCard?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<KanbanCard?, CancellationToken, bool> match, Func<KanbanCard?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(KanbanCard? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<KanbanCard?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<KanbanCard?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(KanbanCard? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<KanbanCard?, CancellationToken, bool> Match, Func<KanbanCard?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<KanbanCard?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<KanbanCard?, CancellationToken, bool> match, Func<KanbanCard?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(KanbanCard? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<KanbanCard?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByAccountEmailAsyncCalls.Count + FindByEmailAndIdAsyncCalls.Count + DeleteByEmailAndIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Clear();
        lock (FindByEmailAndIdAsyncCalls) FindByEmailAndIdAsyncCalls.Clear();
        lock (DeleteByEmailAndIdAsyncCalls) DeleteByEmailAndIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
