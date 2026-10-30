using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IHtmlParserService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeHtmlParserService : IHtmlParserService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>TransformImageAttributesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, Task<(string, bool)>>? OnTransformImageAttributesAsync { get; set; }

    /// <summary>The arguments of every <c>TransformImageAttributesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Html, Func<string, CancellationToken, Task<HtmlImgPatch?>> PatchFactory, CancellationToken Ct)> TransformImageAttributesAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerTransformImageAttributesAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, bool> Match, Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, Task<(string, bool)>> Answer)> _transformImageAttributesAsyncRules = [];

    /// <summary>Makes every later <c>TransformImageAttributesAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerTransformImageAttributesAsync(Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, Task<(string, bool)>> answer) => AnswerTransformImageAttributesAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>TransformImageAttributesAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerTransformImageAttributesAsync(Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, bool> match, Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, Task<(string, bool)>> answer)
    {
        lock (_transformImageAttributesAsyncRules) _transformImageAttributesAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<(string, bool)> TransformImageAttributesAsync(string html, Func<string, CancellationToken, Task<HtmlImgPatch?>> patchFactory, CancellationToken ct)
    {
        lock (TransformImageAttributesAsyncCalls) TransformImageAttributesAsyncCalls.Add((html, patchFactory, ct));
        Func<string, Func<string, CancellationToken, Task<HtmlImgPatch?>>, CancellationToken, Task<(string, bool)>>? answer = null;
        lock (_transformImageAttributesAsyncRules) answer = _transformImageAttributesAsyncRules.LastOrDefault(rule => rule.Match(html, patchFactory, ct)).Answer;
        if ((answer ?? OnTransformImageAttributesAsync) is { } on)
        {
            return on(html, patchFactory, ct);
        }

        return Strict ? throw new InvalidOperationException("TransformImageAttributesAsync was called without an answer on a strict fake.") : Task.FromResult<(string, bool)>(default);
    }

    /// <summary>What <c>TransformImageAttributes</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, Func<string, HtmlImgPatch?>, string>? OnTransformImageAttributes { get; set; }

    /// <summary>The arguments of every <c>TransformImageAttributes</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Html, Func<string, HtmlImgPatch?> PatchFactory)> TransformImageAttributesCalls { get; } = [];

    /// <summary>The rules <c>AnswerTransformImageAttributes</c> added, newest last.</summary>
    private readonly List<(Func<string, Func<string, HtmlImgPatch?>, bool> Match, Func<string, Func<string, HtmlImgPatch?>, string> Answer)> _transformImageAttributesRules = [];

    /// <summary>Makes every later <c>TransformImageAttributes</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerTransformImageAttributes(Func<string, Func<string, HtmlImgPatch?>, string> answer) => AnswerTransformImageAttributes((_, _) => true, answer);

    /// <summary>Makes a later <c>TransformImageAttributes</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerTransformImageAttributes(Func<string, Func<string, HtmlImgPatch?>, bool> match, Func<string, Func<string, HtmlImgPatch?>, string> answer)
    {
        lock (_transformImageAttributesRules) _transformImageAttributesRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string TransformImageAttributes(string html, Func<string, HtmlImgPatch?> patchFactory)
    {
        lock (TransformImageAttributesCalls) TransformImageAttributesCalls.Add((html, patchFactory));
        Func<string, Func<string, HtmlImgPatch?>, string>? answer = null;
        lock (_transformImageAttributesRules) answer = _transformImageAttributesRules.LastOrDefault(rule => rule.Match(html, patchFactory)).Answer;
        if ((answer ?? OnTransformImageAttributes) is { } on)
        {
            return on(html, patchFactory);
        }

        return Strict ? throw new InvalidOperationException("TransformImageAttributes was called without an answer on a strict fake.") : default!;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => TransformImageAttributesAsyncCalls.Count + TransformImageAttributesCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (TransformImageAttributesAsyncCalls) TransformImageAttributesAsyncCalls.Clear();
        lock (TransformImageAttributesCalls) TransformImageAttributesCalls.Clear();
    }
}
