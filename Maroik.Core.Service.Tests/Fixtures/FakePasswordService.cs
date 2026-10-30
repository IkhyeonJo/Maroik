using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IPasswordService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakePasswordService : IPasswordService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>HashPassword</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnHashPassword { get; set; }

    /// <summary>The arguments of every <c>HashPassword</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> HashPasswordCalls { get; } = [];

    /// <summary>The rules <c>AnswerHashPassword</c> added, newest last.</summary>
    private readonly List<(Func<string, bool> Match, Func<string, string> Answer)> _hashPasswordRules = [];

    /// <summary>Makes every later <c>HashPassword</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerHashPassword(Func<string, string> answer) => AnswerHashPassword((_) => true, answer);

    /// <summary>Makes a later <c>HashPassword</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerHashPassword(Func<string, bool> match, Func<string, string> answer)
    {
        lock (_hashPasswordRules) _hashPasswordRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string HashPassword(string password)
    {
        lock (HashPasswordCalls) HashPasswordCalls.Add(password);
        Func<string, string>? answer = null;
        lock (_hashPasswordRules) answer = _hashPasswordRules.LastOrDefault(rule => rule.Match(password)).Answer;
        if ((answer ?? OnHashPassword) is { } on)
        {
            return on(password);
        }

        return Strict ? throw new InvalidOperationException("HashPassword was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>VerifyPassword</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, bool>? OnVerifyPassword { get; set; }

    /// <summary>The arguments of every <c>VerifyPassword</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Password, string HashedPassword)> VerifyPasswordCalls { get; } = [];

    /// <summary>The rules <c>AnswerVerifyPassword</c> added, newest last.</summary>
    private readonly List<(Func<string, string, bool> Match, Func<string, string, bool> Answer)> _verifyPasswordRules = [];

    /// <summary>Makes every later <c>VerifyPassword</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerVerifyPassword(Func<string, string, bool> answer) => AnswerVerifyPassword((_, _) => true, answer);

    /// <summary>Makes a later <c>VerifyPassword</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerVerifyPassword(Func<string, string, bool> match, Func<string, string, bool> answer)
    {
        lock (_verifyPasswordRules) _verifyPasswordRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        lock (VerifyPasswordCalls) VerifyPasswordCalls.Add((password, hashedPassword));
        Func<string, string, bool>? answer = null;
        lock (_verifyPasswordRules) answer = _verifyPasswordRules.LastOrDefault(rule => rule.Match(password, hashedPassword)).Answer;
        if ((answer ?? OnVerifyPassword) is { } on)
        {
            return on(password, hashedPassword);
        }

        return Strict ? throw new InvalidOperationException("VerifyPassword was called without an answer on a strict fake.") : default;
    }

    /// <summary>What <c>VerifyDummy</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool>? OnVerifyDummy { get; set; }

    /// <summary>The arguments of every <c>VerifyDummy</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> VerifyDummyCalls { get; } = [];

    /// <summary>The rules <c>AnswerVerifyDummy</c> added, newest last.</summary>
    private readonly List<(Func<string, bool> Match, Func<string, bool> Answer)> _verifyDummyRules = [];

    /// <summary>Makes every later <c>VerifyDummy</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerVerifyDummy(Func<string, bool> answer) => AnswerVerifyDummy((_) => true, answer);

    /// <summary>Makes a later <c>VerifyDummy</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerVerifyDummy(Func<string, bool> match, Func<string, bool> answer)
    {
        lock (_verifyDummyRules) _verifyDummyRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public bool VerifyDummy(string password)
    {
        lock (VerifyDummyCalls) VerifyDummyCalls.Add(password);
        Func<string, bool>? answer = null;
        lock (_verifyDummyRules) answer = _verifyDummyRules.LastOrDefault(rule => rule.Match(password)).Answer;
        if ((answer ?? OnVerifyDummy) is { } on)
        {
            return on(password);
        }

        return Strict ? throw new InvalidOperationException("VerifyDummy was called without an answer on a strict fake.") : default;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => HashPasswordCalls.Count + VerifyPasswordCalls.Count + VerifyDummyCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (HashPasswordCalls) HashPasswordCalls.Clear();
        lock (VerifyPasswordCalls) VerifyPasswordCalls.Clear();
        lock (VerifyDummyCalls) VerifyDummyCalls.Clear();
    }
}
