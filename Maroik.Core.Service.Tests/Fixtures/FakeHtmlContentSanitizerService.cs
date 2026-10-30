using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IHtmlContentSanitizerService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeHtmlContentSanitizerService : IHtmlContentSanitizerService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>Sanitize</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnSanitize { get; set; }

    /// <summary>The arguments of every <c>Sanitize</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> SanitizeCalls { get; } = [];

    /// <summary>The rules <c>AnswerSanitize</c> added, newest last.</summary>
    private readonly List<(Func<string, bool> Match, Func<string, string> Answer)> _sanitizeRules = [];

    /// <summary>Makes every later <c>Sanitize</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitize(Func<string, string> answer) => AnswerSanitize((_) => true, answer);

    /// <summary>Makes a later <c>Sanitize</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitize(Func<string, bool> match, Func<string, string> answer)
    {
        lock (_sanitizeRules) _sanitizeRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string Sanitize(string html)
    {
        lock (SanitizeCalls) SanitizeCalls.Add(html);
        Func<string, string>? answer = null;
        lock (_sanitizeRules) answer = _sanitizeRules.LastOrDefault(rule => rule.Match(html)).Answer;
        if ((answer ?? OnSanitize) is { } on)
        {
            return on(html);
        }

        return Strict ? throw new InvalidOperationException("Sanitize was called without an answer on a strict fake.") : default!;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => SanitizeCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (SanitizeCalls) SanitizeCalls.Clear();
    }
}
