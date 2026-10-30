using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IDataProtectionService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeDataProtectionService : IDataProtectionService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>Protect</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ProtectedValue, string, string>? OnProtect { get; set; }

    /// <summary>The arguments of every <c>Protect</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(ProtectedValue Kind, string Plaintext)> ProtectCalls { get; } = [];

    /// <summary>The rules <c>AnswerProtect</c> added, newest last.</summary>
    private readonly List<(Func<ProtectedValue, string, bool> Match, Func<ProtectedValue, string, string> Answer)> _protectRules = [];

    /// <summary>Makes every later <c>Protect</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerProtect(Func<ProtectedValue, string, string> answer) => AnswerProtect((_, _) => true, answer);

    /// <summary>Makes a later <c>Protect</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerProtect(Func<ProtectedValue, string, bool> match, Func<ProtectedValue, string, string> answer)
    {
        lock (_protectRules) _protectRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string Protect(ProtectedValue kind, string plaintext)
    {
        lock (ProtectCalls) ProtectCalls.Add((kind, plaintext));
        Func<ProtectedValue, string, string>? answer = null;
        lock (_protectRules) answer = _protectRules.LastOrDefault(rule => rule.Match(kind, plaintext)).Answer;
        if ((answer ?? OnProtect) is { } on)
        {
            return on(kind, plaintext);
        }

        return Strict ? throw new InvalidOperationException("Protect was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>Unprotect</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ProtectedValue, string, string>? OnUnprotect { get; set; }

    /// <summary>The arguments of every <c>Unprotect</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(ProtectedValue Kind, string ProtectedValue)> UnprotectCalls { get; } = [];

    /// <summary>The rules <c>AnswerUnprotect</c> added, newest last.</summary>
    private readonly List<(Func<ProtectedValue, string, bool> Match, Func<ProtectedValue, string, string> Answer)> _unprotectRules = [];

    /// <summary>Makes every later <c>Unprotect</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUnprotect(Func<ProtectedValue, string, string> answer) => AnswerUnprotect((_, _) => true, answer);

    /// <summary>Makes a later <c>Unprotect</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUnprotect(Func<ProtectedValue, string, bool> match, Func<ProtectedValue, string, string> answer)
    {
        lock (_unprotectRules) _unprotectRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string Unprotect(ProtectedValue kind, string protectedValue)
    {
        lock (UnprotectCalls) UnprotectCalls.Add((kind, protectedValue));
        Func<ProtectedValue, string, string>? answer = null;
        lock (_unprotectRules) answer = _unprotectRules.LastOrDefault(rule => rule.Match(kind, protectedValue)).Answer;
        if ((answer ?? OnUnprotect) is { } on)
        {
            return on(kind, protectedValue);
        }

        return Strict ? throw new InvalidOperationException("Unprotect was called without an answer on a strict fake.") : default!;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => ProtectCalls.Count + UnprotectCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (ProtectCalls) ProtectCalls.Clear();
        lock (UnprotectCalls) UnprotectCalls.Clear();
    }
}
