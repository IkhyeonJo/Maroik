using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IBoardCommentRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeBoardCommentRepository : IBoardCommentRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetCommentCountsByBoardIdsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, int>>>? OnGetCommentCountsByBoardIdsAsync { get; set; }

    /// <summary>The arguments of every <c>GetCommentCountsByBoardIdsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> BoardIds, CancellationToken Ct)> GetCommentCountsByBoardIdsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetCommentCountsByBoardIdsAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<long>, CancellationToken, bool> Match, Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, int>>> Answer)> _getCommentCountsByBoardIdsAsyncRules = [];

    /// <summary>Makes every later <c>GetCommentCountsByBoardIdsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetCommentCountsByBoardIdsAsync(Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, int>>> answer) => AnswerGetCommentCountsByBoardIdsAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetCommentCountsByBoardIdsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetCommentCountsByBoardIdsAsync(Func<IEnumerable<long>, CancellationToken, bool> match, Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, int>>> answer)
    {
        lock (_getCommentCountsByBoardIdsAsyncRules) _getCommentCountsByBoardIdsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct)
    {
        lock (GetCommentCountsByBoardIdsAsyncCalls) GetCommentCountsByBoardIdsAsyncCalls.Add((boardIds, ct));
        Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, int>>>? answer = null;
        lock (_getCommentCountsByBoardIdsAsyncRules) answer = _getCommentCountsByBoardIdsAsyncRules.LastOrDefault(rule => rule.Match(boardIds, ct)).Answer;
        if ((answer ?? OnGetCommentCountsByBoardIdsAsync) is { } on)
        {
            return on(boardIds, ct);
        }

        return Strict ? throw new InvalidOperationException("GetCommentCountsByBoardIdsAsync was called without an answer on a strict fake.") : Task.FromResult<Dictionary<long, int>>([]);
    }

    /// <summary>What <c>GetByBoardIdOrderedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<List<BoardComment>>>? OnGetByBoardIdOrderedAsync { get; set; }

    /// <summary>The arguments of every <c>GetByBoardIdOrderedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long BoardId, CancellationToken Ct)> GetByBoardIdOrderedAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByBoardIdOrderedAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<List<BoardComment>>> Answer)> _getByBoardIdOrderedAsyncRules = [];

    /// <summary>Makes every later <c>GetByBoardIdOrderedAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByBoardIdOrderedAsync(Func<long, CancellationToken, Task<List<BoardComment>>> answer) => AnswerGetByBoardIdOrderedAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByBoardIdOrderedAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByBoardIdOrderedAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<List<BoardComment>>> answer)
    {
        lock (_getByBoardIdOrderedAsyncRules) _getByBoardIdOrderedAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<BoardComment>> GetByBoardIdOrderedAsync(long boardId, CancellationToken ct)
    {
        lock (GetByBoardIdOrderedAsyncCalls) GetByBoardIdOrderedAsyncCalls.Add((boardId, ct));
        Func<long, CancellationToken, Task<List<BoardComment>>>? answer = null;
        lock (_getByBoardIdOrderedAsyncRules) answer = _getByBoardIdOrderedAsyncRules.LastOrDefault(rule => rule.Match(boardId, ct)).Answer;
        if ((answer ?? OnGetByBoardIdOrderedAsync) is { } on)
        {
            return on(boardId, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByBoardIdOrderedAsync was called without an answer on a strict fake.") : Task.FromResult<List<BoardComment>>([]);
    }

    /// <summary>What <c>FindByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<BoardComment?>>? OnFindByIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<BoardComment?>> Answer)> _findByIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, Task<BoardComment?>> answer) => AnswerFindByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<BoardComment?>> answer)
    {
        lock (_findByIdAsyncRules) _findByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<BoardComment?> FindByIdAsync(long id, CancellationToken ct)
    {
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<BoardComment?>>? answer = null;
        lock (_findByIdAsyncRules) answer = _findByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdAsync was called without an answer on a strict fake.") : Task.FromResult<BoardComment?>(default);
    }

    /// <summary>What <c>FindByIdForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<BoardComment?>>? OnFindByIdForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByIdForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindByIdForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByIdForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<BoardComment?>> Answer)> _findByIdForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByIdForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, Task<BoardComment?>> answer) => AnswerFindByIdForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByIdForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByIdForUpdateAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<BoardComment?>> answer)
    {
        lock (_findByIdForUpdateAsyncRules) _findByIdForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<BoardComment?> FindByIdForUpdateAsync(long id, CancellationToken ct)
    {
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<BoardComment?>>? answer = null;
        lock (_findByIdForUpdateAsyncRules) answer = _findByIdForUpdateAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindByIdForUpdateAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByIdForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<BoardComment?>(default);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<BoardComment?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(BoardComment? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<BoardComment?, CancellationToken, bool> Match, Func<BoardComment?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<BoardComment?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<BoardComment?, CancellationToken, bool> match, Func<BoardComment?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(BoardComment? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<BoardComment?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<BoardComment?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(BoardComment? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<BoardComment?, CancellationToken, bool> Match, Func<BoardComment?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<BoardComment?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<BoardComment?, CancellationToken, bool> match, Func<BoardComment?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(BoardComment? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<BoardComment?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetCommentCountsByBoardIdsAsyncCalls.Count + GetByBoardIdOrderedAsyncCalls.Count + FindByIdAsyncCalls.Count + FindByIdForUpdateAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetCommentCountsByBoardIdsAsyncCalls) GetCommentCountsByBoardIdsAsyncCalls.Clear();
        lock (GetByBoardIdOrderedAsyncCalls) GetByBoardIdOrderedAsyncCalls.Clear();
        lock (FindByIdAsyncCalls) FindByIdAsyncCalls.Clear();
        lock (FindByIdForUpdateAsyncCalls) FindByIdForUpdateAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
