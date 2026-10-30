using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Menus;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICategoryRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCategoryRepository : ICategoryRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetAllAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Category>>>? OnGetAllAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAllAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Category>>> Answer)> _getAllAsyncRules = [];

    /// <summary>Makes every later <c>GetAllAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllAsync(Func<CancellationToken, Task<List<Category>>> answer) => AnswerGetAllAsync((_) => true, answer);

    /// <summary>Makes a later <c>GetAllAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Category>>> answer)
    {
        lock (_getAllAsyncRules) _getAllAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Category>> GetAllAsync(CancellationToken ct)
    {
        lock (GetAllAsyncCalls) GetAllAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Category>>>? answer = null;
        lock (_getAllAsyncRules) answer = _getAllAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnGetAllAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("GetAllAsync was called without an answer on a strict fake.") : Task.FromResult<List<Category>>([]);
    }

    /// <summary>What <c>FindByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Category?>>? OnFindByIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Category?>> Answer)> _findByIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, Task<Category?>> answer) => AnswerFindByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Category?>> answer)
    {
        lock (_findByIdAsyncRules) _findByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Category?> FindByIdAsync(long id, CancellationToken ct)
    {
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Category?>>? answer = null;
        lock (_findByIdAsyncRules) answer = _findByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdAsync was called without an answer on a strict fake.") : Task.FromResult<Category?>(default);
    }

    /// <summary>What <c>FindByIdForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Category?>>? OnFindByIdForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Category?>> Answer)> _findByIdForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, Task<Category?>> answer) => AnswerFindByIdForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Category?>> answer)
    {
        lock (_findByIdForUpdateAsyncRules) _findByIdForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Category?> FindByIdForUpdateAsync(long id, CancellationToken ct)
    {
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Category?>>? answer = null;
        lock (_findByIdForUpdateAsyncRules) answer = _findByIdForUpdateAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdForUpdateAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<Category?>(default);
    }

    /// <summary>What <c>GetAllForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Category>>>? OnGetAllForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAllForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Category>>> Answer)> _getAllForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>GetAllForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllForUpdateAsync(Func<CancellationToken, Task<List<Category>>> answer) => AnswerGetAllForUpdateAsync((_) => true, answer);

    /// <summary>Makes a later <c>GetAllForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllForUpdateAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Category>>> answer)
    {
        lock (_getAllForUpdateAsyncRules) _getAllForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Category>> GetAllForUpdateAsync(CancellationToken ct)
    {
        lock (GetAllForUpdateAsyncCalls) GetAllForUpdateAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Category>>>? answer = null;
        lock (_getAllForUpdateAsyncRules) answer = _getAllForUpdateAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnGetAllForUpdateAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("GetAllForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<List<Category>>([]);
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
    public Func<Category?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Category? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<Category?, CancellationToken, bool> Match, Func<Category?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Category?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Category?, CancellationToken, bool> match, Func<Category?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(Category? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<Category?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Category?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Category? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<Category?, CancellationToken, bool> Match, Func<Category?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Category?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Category?, CancellationToken, bool> match, Func<Category?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(Category? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<Category?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetAllAsyncCalls.Count + FindByIdAsyncCalls.Count + FindByIdForUpdateAsyncCalls.Count + GetAllForUpdateAsyncCalls.Count + DeleteByIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetAllAsyncCalls) GetAllAsyncCalls.Clear();
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Clear();
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Clear();
        lock (GetAllForUpdateAsyncCalls) GetAllForUpdateAsyncCalls.Clear();
        lock (DeleteByIdAsyncCalls) DeleteByIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
