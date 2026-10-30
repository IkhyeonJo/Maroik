using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAccountRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAccountRepository : IAccountRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetAllAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Account>>>? OnGetAllAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAllAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Account>>> Answer)> _getAllAsyncRules = [];

    /// <summary>Makes every later <c>GetAllAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllAsync(Func<CancellationToken, Task<List<Account>>> answer) => AnswerGetAllAsync((_) => true, answer);

    /// <summary>Makes a later <c>GetAllAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAllAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Account>>> answer)
    {
        lock (_getAllAsyncRules) _getAllAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Account>> GetAllAsync(CancellationToken ct)
    {
        lock (GetAllAsyncCalls) GetAllAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Account>>>? answer = null;
        lock (_getAllAsyncRules) answer = _getAllAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnGetAllAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("GetAllAsync was called without an answer on a strict fake.") : Task.FromResult<List<Account>>([]);
    }

    /// <summary>What <c>SearchAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<Account>>>? OnSearchAsync { get; set; }

    /// <summary>The arguments of every <c>SearchAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Search, CancellationToken Ct)> SearchAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerSearchAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<Account>>> Answer)> _searchAsyncRules = [];

    /// <summary>Makes every later <c>SearchAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchAsync(Func<string, CancellationToken, Task<List<Account>>> answer) => AnswerSearchAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>SearchAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<Account>>> answer)
    {
        lock (_searchAsyncRules) _searchAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Account>> SearchAsync(string search, CancellationToken ct)
    {
        lock (SearchAsyncCalls) SearchAsyncCalls.Add((search, ct));
        Func<string, CancellationToken, Task<List<Account>>>? answer = null;
        lock (_searchAsyncRules) answer = _searchAsyncRules.LastOrDefault(rule => rule.Match(search, ct)).Answer;
        if ((answer ?? OnSearchAsync) is { } on)
        {
            return on(search, ct);
        }

        return Strict ? throw new InvalidOperationException("SearchAsync was called without an answer on a strict fake.") : Task.FromResult<List<Account>>([]);
    }

    /// <summary>What <c>FindByEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<Account?>>? OnFindByEmailAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> FindByEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<Account?>> Answer)> _findByEmailAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAsync(Func<string, CancellationToken, Task<Account?>> answer) => AnswerFindByEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<Account?>> answer)
    {
        lock (_findByEmailAsyncRules) _findByEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Account?> FindByEmailAsync(string email, CancellationToken ct)
    {
        lock (FindByEmailAsyncCalls) FindByEmailAsyncCalls.Add((email, ct));
        Func<string, CancellationToken, Task<Account?>>? answer = null;
        lock (_findByEmailAsyncRules) answer = _findByEmailAsyncRules.LastOrDefault(rule => rule.Match(email, ct)).Answer;
        if ((answer ?? OnFindByEmailAsync) is { } on)
        {
            return on(email, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAsync was called without an answer on a strict fake.") : Task.FromResult<Account?>(default);
    }

    /// <summary>What <c>FindByRegistrationTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<Account?>>? OnFindByRegistrationTokenAsync { get; set; }

    /// <summary>The arguments of every <c>FindByRegistrationTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Token, CancellationToken Ct)> FindByRegistrationTokenAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByRegistrationTokenAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<Account?>> Answer)> _findByRegistrationTokenAsyncRules = [];

    /// <summary>Makes every later <c>FindByRegistrationTokenAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByRegistrationTokenAsync(Func<string, CancellationToken, Task<Account?>> answer) => AnswerFindByRegistrationTokenAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByRegistrationTokenAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByRegistrationTokenAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<Account?>> answer)
    {
        lock (_findByRegistrationTokenAsyncRules) _findByRegistrationTokenAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Account?> FindByRegistrationTokenAsync(string token, CancellationToken ct)
    {
        lock (FindByRegistrationTokenAsyncCalls) FindByRegistrationTokenAsyncCalls.Add((token, ct));
        Func<string, CancellationToken, Task<Account?>>? answer = null;
        lock (_findByRegistrationTokenAsyncRules) answer = _findByRegistrationTokenAsyncRules.LastOrDefault(rule => rule.Match(token, ct)).Answer;
        if ((answer ?? OnFindByRegistrationTokenAsync) is { } on)
        {
            return on(token, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByRegistrationTokenAsync was called without an answer on a strict fake.") : Task.FromResult<Account?>(default);
    }

    /// <summary>What <c>FindByResetPasswordTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<Account?>>? OnFindByResetPasswordTokenAsync { get; set; }

    /// <summary>The arguments of every <c>FindByResetPasswordTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Token, CancellationToken Ct)> FindByResetPasswordTokenAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByResetPasswordTokenAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<Account?>> Answer)> _findByResetPasswordTokenAsyncRules = [];

    /// <summary>Makes every later <c>FindByResetPasswordTokenAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByResetPasswordTokenAsync(Func<string, CancellationToken, Task<Account?>> answer) => AnswerFindByResetPasswordTokenAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByResetPasswordTokenAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByResetPasswordTokenAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<Account?>> answer)
    {
        lock (_findByResetPasswordTokenAsyncRules) _findByResetPasswordTokenAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Account?> FindByResetPasswordTokenAsync(string token, CancellationToken ct)
    {
        lock (FindByResetPasswordTokenAsyncCalls) FindByResetPasswordTokenAsyncCalls.Add((token, ct));
        Func<string, CancellationToken, Task<Account?>>? answer = null;
        lock (_findByResetPasswordTokenAsyncRules) answer = _findByResetPasswordTokenAsyncRules.LastOrDefault(rule => rule.Match(token, ct)).Answer;
        if ((answer ?? OnFindByResetPasswordTokenAsync) is { } on)
        {
            return on(token, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByResetPasswordTokenAsync was called without an answer on a strict fake.") : Task.FromResult<Account?>(default);
    }

    /// <summary>What <c>NicknameExistsIgnoreCaseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<bool>>? OnNicknameExistsIgnoreCaseAsync { get; set; }

    /// <summary>The arguments of every <c>NicknameExistsIgnoreCaseAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Nickname, CancellationToken Ct)> NicknameExistsIgnoreCaseAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerNicknameExistsIgnoreCaseAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<bool>> Answer)> _nicknameExistsIgnoreCaseAsyncRules = [];

    /// <summary>Makes every later <c>NicknameExistsIgnoreCaseAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerNicknameExistsIgnoreCaseAsync(Func<string, CancellationToken, Task<bool>> answer) => AnswerNicknameExistsIgnoreCaseAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>NicknameExistsIgnoreCaseAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerNicknameExistsIgnoreCaseAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<bool>> answer)
    {
        lock (_nicknameExistsIgnoreCaseAsyncRules) _nicknameExistsIgnoreCaseAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<bool> NicknameExistsIgnoreCaseAsync(string nickname, CancellationToken ct)
    {
        lock (NicknameExistsIgnoreCaseAsyncCalls) NicknameExistsIgnoreCaseAsyncCalls.Add((nickname, ct));
        Func<string, CancellationToken, Task<bool>>? answer = null;
        lock (_nicknameExistsIgnoreCaseAsyncRules) answer = _nicknameExistsIgnoreCaseAsyncRules.LastOrDefault(rule => rule.Match(nickname, ct)).Answer;
        if ((answer ?? OnNicknameExistsIgnoreCaseAsync) is { } on)
        {
            return on(nickname, ct);
        }

        return Strict ? throw new InvalidOperationException("NicknameExistsIgnoreCaseAsync was called without an answer on a strict fake.") : Task.FromResult<bool>(default);
    }

    /// <summary>What <c>FindByNicknamesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<string>, CancellationToken, Task<List<Account>>>? OnFindByNicknamesAsync { get; set; }

    /// <summary>The arguments of every <c>FindByNicknamesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<string> Nicknames, CancellationToken Ct)> FindByNicknamesAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByNicknamesAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<string>, CancellationToken, bool> Match, Func<IEnumerable<string>, CancellationToken, Task<List<Account>>> Answer)> _findByNicknamesAsyncRules = [];

    /// <summary>Makes every later <c>FindByNicknamesAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByNicknamesAsync(Func<IEnumerable<string>, CancellationToken, Task<List<Account>>> answer) => AnswerFindByNicknamesAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByNicknamesAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByNicknamesAsync(Func<IEnumerable<string>, CancellationToken, bool> match, Func<IEnumerable<string>, CancellationToken, Task<List<Account>>> answer)
    {
        lock (_findByNicknamesAsyncRules) _findByNicknamesAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Account>> FindByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct)
    {
        lock (FindByNicknamesAsyncCalls) FindByNicknamesAsyncCalls.Add((nicknames, ct));
        Func<IEnumerable<string>, CancellationToken, Task<List<Account>>>? answer = null;
        lock (_findByNicknamesAsyncRules) answer = _findByNicknamesAsyncRules.LastOrDefault(rule => rule.Match(nicknames, ct)).Answer;
        if ((answer ?? OnFindByNicknamesAsync) is { } on)
        {
            return on(nicknames, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByNicknamesAsync was called without an answer on a strict fake.") : Task.FromResult<List<Account>>([]);
    }

    /// <summary>What <c>FindByEmailForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<Account?>>? OnFindByEmailForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> FindByEmailForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<Account?>> Answer)> _findByEmailForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailForUpdateAsync(Func<string, CancellationToken, Task<Account?>> answer) => AnswerFindByEmailForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailForUpdateAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<Account?>> answer)
    {
        lock (_findByEmailForUpdateAsyncRules) _findByEmailForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Account?> FindByEmailForUpdateAsync(string email, CancellationToken ct)
    {
        lock (FindByEmailForUpdateAsyncCalls) FindByEmailForUpdateAsyncCalls.Add((email, ct));
        Func<string, CancellationToken, Task<Account?>>? answer = null;
        lock (_findByEmailForUpdateAsyncRules) answer = _findByEmailForUpdateAsyncRules.LastOrDefault(rule => rule.Match(email, ct)).Answer;
        if ((answer ?? OnFindByEmailForUpdateAsync) is { } on)
        {
            return on(email, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<Account?>(default);
    }

    /// <summary>What <c>FindActiveAdminsForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<Account>>>? OnFindActiveAdminsForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindActiveAdminsForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> FindActiveAdminsForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindActiveAdminsForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task<List<Account>>> Answer)> _findActiveAdminsForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindActiveAdminsForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveAdminsForUpdateAsync(Func<CancellationToken, Task<List<Account>>> answer) => AnswerFindActiveAdminsForUpdateAsync((_) => true, answer);

    /// <summary>Makes a later <c>FindActiveAdminsForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveAdminsForUpdateAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task<List<Account>>> answer)
    {
        lock (_findActiveAdminsForUpdateAsyncRules) _findActiveAdminsForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Account>> FindActiveAdminsForUpdateAsync(CancellationToken ct)
    {
        lock (FindActiveAdminsForUpdateAsyncCalls) FindActiveAdminsForUpdateAsyncCalls.Add(ct);
        Func<CancellationToken, Task<List<Account>>>? answer = null;
        lock (_findActiveAdminsForUpdateAsyncRules) answer = _findActiveAdminsForUpdateAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnFindActiveAdminsForUpdateAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("FindActiveAdminsForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<List<Account>>([]);
    }

    /// <summary>What <c>UpdateAvatarPathAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, DateTime, CancellationToken, Task<int>>? OnUpdateAvatarPathAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateAvatarPathAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? AvatarImagePath, DateTime Updated, CancellationToken Ct)> UpdateAvatarPathAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateAvatarPathAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, DateTime, CancellationToken, bool> Match, Func<string, string?, DateTime, CancellationToken, Task<int>> Answer)> _updateAvatarPathAsyncRules = [];

    /// <summary>Makes every later <c>UpdateAvatarPathAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAvatarPathAsync(Func<string, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateAvatarPathAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateAvatarPathAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAvatarPathAsync(Func<string, string?, DateTime, CancellationToken, bool> match, Func<string, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateAvatarPathAsyncRules) _updateAvatarPathAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateAvatarPathAsync(string email, string? avatarImagePath, DateTime updated, CancellationToken ct)
    {
        lock (UpdateAvatarPathAsyncCalls) UpdateAvatarPathAsyncCalls.Add((email, avatarImagePath, updated, ct));
        Func<string, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateAvatarPathAsyncRules) answer = _updateAvatarPathAsyncRules.LastOrDefault(rule => rule.Match(email, avatarImagePath, updated, ct)).Answer;
        if ((answer ?? OnUpdateAvatarPathAsync) is { } on)
        {
            return on(email, avatarImagePath, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateAvatarPathAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateTimeZoneAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, DateTime, CancellationToken, Task<int>>? OnUpdateTimeZoneAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateTimeZoneAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string TimeZoneIanaId, DateTime Updated, CancellationToken Ct)> UpdateTimeZoneAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateTimeZoneAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, DateTime, CancellationToken, bool> Match, Func<string, string, DateTime, CancellationToken, Task<int>> Answer)> _updateTimeZoneAsyncRules = [];

    /// <summary>Makes every later <c>UpdateTimeZoneAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateTimeZoneAsync(Func<string, string, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateTimeZoneAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateTimeZoneAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateTimeZoneAsync(Func<string, string, DateTime, CancellationToken, bool> match, Func<string, string, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateTimeZoneAsyncRules) _updateTimeZoneAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateTimeZoneAsync(string email, string timeZoneIanaId, DateTime updated, CancellationToken ct)
    {
        lock (UpdateTimeZoneAsyncCalls) UpdateTimeZoneAsyncCalls.Add((email, timeZoneIanaId, updated, ct));
        Func<string, string, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateTimeZoneAsyncRules) answer = _updateTimeZoneAsyncRules.LastOrDefault(rule => rule.Match(email, timeZoneIanaId, updated, ct)).Answer;
        if ((answer ?? OnUpdateTimeZoneAsync) is { } on)
        {
            return on(email, timeZoneIanaId, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateTimeZoneAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdatePasswordAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, Task<int>>? OnUpdatePasswordAsync { get; set; }

    /// <summary>The arguments of every <c>UpdatePasswordAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string HashedPassword, string SecurityStamp, bool MustChangePassword, string? ResetPasswordToken, bool Locked, long LoginAttempt, string? Message, DateTime Updated, CancellationToken Ct)> UpdatePasswordAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdatePasswordAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, bool> Match, Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, Task<int>> Answer)> _updatePasswordAsyncRules = [];

    /// <summary>Makes every later <c>UpdatePasswordAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdatePasswordAsync(Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdatePasswordAsync((_, _, _, _, _, _, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdatePasswordAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdatePasswordAsync(Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, bool> match, Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updatePasswordAsyncRules) _updatePasswordAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdatePasswordAsync(string email, string hashedPassword, string securityStamp, bool mustChangePassword, string? resetPasswordToken, bool locked, long loginAttempt, string? message, DateTime updated, CancellationToken ct)
    {
        lock (UpdatePasswordAsyncCalls) UpdatePasswordAsyncCalls.Add((email, hashedPassword, securityStamp, mustChangePassword, resetPasswordToken, locked, loginAttempt, message, updated, ct));
        Func<string, string, string, bool, string?, bool, long, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updatePasswordAsyncRules) answer = _updatePasswordAsyncRules.LastOrDefault(rule => rule.Match(email, hashedPassword, securityStamp, mustChangePassword, resetPasswordToken, locked, loginAttempt, message, updated, ct)).Answer;
        if ((answer ?? OnUpdatePasswordAsync) is { } on)
        {
            return on(email, hashedPassword, securityStamp, mustChangePassword, resetPasswordToken, locked, loginAttempt, message, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdatePasswordAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateDefaultMonetaryUnitAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, CancellationToken, Task<int>>? OnUpdateDefaultMonetaryUnitAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateDefaultMonetaryUnitAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? DefaultMonetaryUnit, CancellationToken Ct)> UpdateDefaultMonetaryUnitAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateDefaultMonetaryUnitAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, CancellationToken, bool> Match, Func<string, string?, CancellationToken, Task<int>> Answer)> _updateDefaultMonetaryUnitAsyncRules = [];

    /// <summary>Makes every later <c>UpdateDefaultMonetaryUnitAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateDefaultMonetaryUnitAsync(Func<string, string?, CancellationToken, Task<int>> answer) => AnswerUpdateDefaultMonetaryUnitAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateDefaultMonetaryUnitAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateDefaultMonetaryUnitAsync(Func<string, string?, CancellationToken, bool> match, Func<string, string?, CancellationToken, Task<int>> answer)
    {
        lock (_updateDefaultMonetaryUnitAsyncRules) _updateDefaultMonetaryUnitAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateDefaultMonetaryUnitAsync(string email, string? defaultMonetaryUnit, CancellationToken ct)
    {
        lock (UpdateDefaultMonetaryUnitAsyncCalls) UpdateDefaultMonetaryUnitAsyncCalls.Add((email, defaultMonetaryUnit, ct));
        Func<string, string?, CancellationToken, Task<int>>? answer = null;
        lock (_updateDefaultMonetaryUnitAsyncRules) answer = _updateDefaultMonetaryUnitAsyncRules.LastOrDefault(rule => rule.Match(email, defaultMonetaryUnit, ct)).Answer;
        if ((answer ?? OnUpdateDefaultMonetaryUnitAsync) is { } on)
        {
            return on(email, defaultMonetaryUnit, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateDefaultMonetaryUnitAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateResetPasswordTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, DateTime, CancellationToken, Task<int>>? OnUpdateResetPasswordTokenAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateResetPasswordTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? ResetPasswordToken, DateTime Updated, CancellationToken Ct)> UpdateResetPasswordTokenAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateResetPasswordTokenAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, DateTime, CancellationToken, bool> Match, Func<string, string?, DateTime, CancellationToken, Task<int>> Answer)> _updateResetPasswordTokenAsyncRules = [];

    /// <summary>Makes every later <c>UpdateResetPasswordTokenAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateResetPasswordTokenAsync(Func<string, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateResetPasswordTokenAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateResetPasswordTokenAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateResetPasswordTokenAsync(Func<string, string?, DateTime, CancellationToken, bool> match, Func<string, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateResetPasswordTokenAsyncRules) _updateResetPasswordTokenAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateResetPasswordTokenAsync(string email, string? resetPasswordToken, DateTime updated, CancellationToken ct)
    {
        lock (UpdateResetPasswordTokenAsyncCalls) UpdateResetPasswordTokenAsyncCalls.Add((email, resetPasswordToken, updated, ct));
        Func<string, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateResetPasswordTokenAsyncRules) answer = _updateResetPasswordTokenAsyncRules.LastOrDefault(rule => rule.Match(email, resetPasswordToken, updated, ct)).Answer;
        if ((answer ?? OnUpdateResetPasswordTokenAsync) is { } on)
        {
            return on(email, resetPasswordToken, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateResetPasswordTokenAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateRegistrationTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, DateTime, CancellationToken, Task<int>>? OnUpdateRegistrationTokenAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateRegistrationTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? RegistrationToken, DateTime Updated, CancellationToken Ct)> UpdateRegistrationTokenAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateRegistrationTokenAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, DateTime, CancellationToken, bool> Match, Func<string, string?, DateTime, CancellationToken, Task<int>> Answer)> _updateRegistrationTokenAsyncRules = [];

    /// <summary>Makes every later <c>UpdateRegistrationTokenAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateRegistrationTokenAsync(Func<string, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateRegistrationTokenAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateRegistrationTokenAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateRegistrationTokenAsync(Func<string, string?, DateTime, CancellationToken, bool> match, Func<string, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateRegistrationTokenAsyncRules) _updateRegistrationTokenAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateRegistrationTokenAsync(string email, string? registrationToken, DateTime updated, CancellationToken ct)
    {
        lock (UpdateRegistrationTokenAsyncCalls) UpdateRegistrationTokenAsyncCalls.Add((email, registrationToken, updated, ct));
        Func<string, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateRegistrationTokenAsyncRules) answer = _updateRegistrationTokenAsyncRules.LastOrDefault(rule => rule.Match(email, registrationToken, updated, ct)).Answer;
        if ((answer ?? OnUpdateRegistrationTokenAsync) is { } on)
        {
            return on(email, registrationToken, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateRegistrationTokenAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateAgreedServiceTermsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, DateTime, CancellationToken, Task<int>>? OnUpdateAgreedServiceTermsAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateAgreedServiceTermsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, bool AgreedServiceTerms, DateTime Updated, CancellationToken Ct)> UpdateAgreedServiceTermsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateAgreedServiceTermsAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, bool, DateTime, CancellationToken, bool> Match, Func<string, bool, DateTime, CancellationToken, Task<int>> Answer)> _updateAgreedServiceTermsAsyncRules = [];

    /// <summary>Makes every later <c>UpdateAgreedServiceTermsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAgreedServiceTermsAsync(Func<string, bool, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateAgreedServiceTermsAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateAgreedServiceTermsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAgreedServiceTermsAsync(Func<string, bool, DateTime, CancellationToken, bool> match, Func<string, bool, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateAgreedServiceTermsAsyncRules) _updateAgreedServiceTermsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateAgreedServiceTermsAsync(string email, bool agreedServiceTerms, DateTime updated, CancellationToken ct)
    {
        lock (UpdateAgreedServiceTermsAsyncCalls) UpdateAgreedServiceTermsAsyncCalls.Add((email, agreedServiceTerms, updated, ct));
        Func<string, bool, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateAgreedServiceTermsAsyncRules) answer = _updateAgreedServiceTermsAsyncRules.LastOrDefault(rule => rule.Match(email, agreedServiceTerms, updated, ct)).Answer;
        if ((answer ?? OnUpdateAgreedServiceTermsAsync) is { } on)
        {
            return on(email, agreedServiceTerms, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateAgreedServiceTermsAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateMessageAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, DateTime, CancellationToken, Task<int>>? OnUpdateMessageAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateMessageAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? Message, DateTime Updated, CancellationToken Ct)> UpdateMessageAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateMessageAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, DateTime, CancellationToken, bool> Match, Func<string, string?, DateTime, CancellationToken, Task<int>> Answer)> _updateMessageAsyncRules = [];

    /// <summary>Makes every later <c>UpdateMessageAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateMessageAsync(Func<string, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateMessageAsync((_, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateMessageAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateMessageAsync(Func<string, string?, DateTime, CancellationToken, bool> match, Func<string, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateMessageAsyncRules) _updateMessageAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateMessageAsync(string email, string? message, DateTime updated, CancellationToken ct)
    {
        lock (UpdateMessageAsyncCalls) UpdateMessageAsyncCalls.Add((email, message, updated, ct));
        Func<string, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateMessageAsyncRules) answer = _updateMessageAsyncRules.LastOrDefault(rule => rule.Match(email, message, updated, ct)).Answer;
        if ((answer ?? OnUpdateMessageAsync) is { } on)
        {
            return on(email, message, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateMessageAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>UpdateEmailConfirmationAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, string?, DateTime, CancellationToken, Task<int>>? OnUpdateEmailConfirmationAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEmailConfirmationAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string? RegistrationToken, string? Message, DateTime Updated, CancellationToken Ct)> UpdateEmailConfirmationAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEmailConfirmationAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string?, string?, DateTime, CancellationToken, bool> Match, Func<string, string?, string?, DateTime, CancellationToken, Task<int>> Answer)> _updateEmailConfirmationAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEmailConfirmationAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEmailConfirmationAsync(Func<string, string?, string?, DateTime, CancellationToken, Task<int>> answer) => AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEmailConfirmationAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEmailConfirmationAsync(Func<string, string?, string?, DateTime, CancellationToken, bool> match, Func<string, string?, string?, DateTime, CancellationToken, Task<int>> answer)
    {
        lock (_updateEmailConfirmationAsyncRules) _updateEmailConfirmationAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateEmailConfirmationAsync(string email, string? registrationToken, string? message, DateTime updated, CancellationToken ct)
    {
        lock (UpdateEmailConfirmationAsyncCalls) UpdateEmailConfirmationAsyncCalls.Add((email, registrationToken, message, updated, ct));
        Func<string, string?, string?, DateTime, CancellationToken, Task<int>>? answer = null;
        lock (_updateEmailConfirmationAsyncRules) answer = _updateEmailConfirmationAsyncRules.LastOrDefault(rule => rule.Match(email, registrationToken, message, updated, ct)).Answer;
        if ((answer ?? OnUpdateEmailConfirmationAsync) is { } on)
        {
            return on(email, registrationToken, message, updated, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEmailConfirmationAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Account?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Account? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<Account?, CancellationToken, bool> Match, Func<Account?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Account?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Account?, CancellationToken, bool> match, Func<Account?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(Account? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<Account?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Account?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Account? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<Account?, CancellationToken, bool> Match, Func<Account?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Account?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Account?, CancellationToken, bool> match, Func<Account?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(Account? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<Account?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetAllAsyncCalls.Count + SearchAsyncCalls.Count + FindByEmailAsyncCalls.Count + FindByRegistrationTokenAsyncCalls.Count + FindByResetPasswordTokenAsyncCalls.Count + NicknameExistsIgnoreCaseAsyncCalls.Count + FindByNicknamesAsyncCalls.Count + FindByEmailForUpdateAsyncCalls.Count + FindActiveAdminsForUpdateAsyncCalls.Count + UpdateAvatarPathAsyncCalls.Count + UpdateTimeZoneAsyncCalls.Count + UpdatePasswordAsyncCalls.Count + UpdateDefaultMonetaryUnitAsyncCalls.Count + UpdateResetPasswordTokenAsyncCalls.Count + UpdateRegistrationTokenAsyncCalls.Count + UpdateAgreedServiceTermsAsyncCalls.Count + UpdateMessageAsyncCalls.Count + UpdateEmailConfirmationAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetAllAsyncCalls) GetAllAsyncCalls.Clear();
        lock (SearchAsyncCalls) SearchAsyncCalls.Clear();
        lock (FindByEmailAsyncCalls) FindByEmailAsyncCalls.Clear();
        lock (FindByRegistrationTokenAsyncCalls) FindByRegistrationTokenAsyncCalls.Clear();
        lock (FindByResetPasswordTokenAsyncCalls) FindByResetPasswordTokenAsyncCalls.Clear();
        lock (NicknameExistsIgnoreCaseAsyncCalls) NicknameExistsIgnoreCaseAsyncCalls.Clear();
        lock (FindByNicknamesAsyncCalls) FindByNicknamesAsyncCalls.Clear();
        lock (FindByEmailForUpdateAsyncCalls) FindByEmailForUpdateAsyncCalls.Clear();
        lock (FindActiveAdminsForUpdateAsyncCalls) FindActiveAdminsForUpdateAsyncCalls.Clear();
        lock (UpdateAvatarPathAsyncCalls) UpdateAvatarPathAsyncCalls.Clear();
        lock (UpdateTimeZoneAsyncCalls) UpdateTimeZoneAsyncCalls.Clear();
        lock (UpdatePasswordAsyncCalls) UpdatePasswordAsyncCalls.Clear();
        lock (UpdateDefaultMonetaryUnitAsyncCalls) UpdateDefaultMonetaryUnitAsyncCalls.Clear();
        lock (UpdateResetPasswordTokenAsyncCalls) UpdateResetPasswordTokenAsyncCalls.Clear();
        lock (UpdateRegistrationTokenAsyncCalls) UpdateRegistrationTokenAsyncCalls.Clear();
        lock (UpdateAgreedServiceTermsAsyncCalls) UpdateAgreedServiceTermsAsyncCalls.Clear();
        lock (UpdateMessageAsyncCalls) UpdateMessageAsyncCalls.Clear();
        lock (UpdateEmailConfirmationAsyncCalls) UpdateEmailConfirmationAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
