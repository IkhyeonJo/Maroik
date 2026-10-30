using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IImageValidatorService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeImageValidatorService : IImageValidatorService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>IsValidImage</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<byte[], bool>? OnIsValidImage { get; set; }

    /// <summary>The arguments of every <c>IsValidImage</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<byte[]> IsValidImageCalls { get; } = [];

    /// <summary>The rules <c>AnswerIsValidImage</c> added, newest last.</summary>
    private readonly List<(Func<byte[], bool> Match, Func<byte[], bool> Answer)> _isValidImageRules = [];

    /// <summary>Makes every later <c>IsValidImage</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIsValidImage(Func<byte[], bool> answer) => AnswerIsValidImage((_) => true, answer);

    /// <summary>Makes a later <c>IsValidImage</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIsValidImage(Func<byte[], bool> match, Func<byte[], bool> answer)
    {
        lock (_isValidImageRules) _isValidImageRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public bool IsValidImage(byte[] bytes)
    {
        lock (IsValidImageCalls) IsValidImageCalls.Add(bytes);
        Func<byte[], bool>? answer = null;
        lock (_isValidImageRules) answer = _isValidImageRules.LastOrDefault(rule => rule.Match(bytes)).Answer;
        if ((answer ?? OnIsValidImage) is { } on)
        {
            return on(bytes);
        }

        return Strict ? throw new InvalidOperationException("IsValidImage was called without an answer on a strict fake.") : default;
    }

    /// <summary>What <c>IsSvg</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<byte[], bool>? OnIsSvg { get; set; }

    /// <summary>The arguments of every <c>IsSvg</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<byte[]> IsSvgCalls { get; } = [];

    /// <summary>The rules <c>AnswerIsSvg</c> added, newest last.</summary>
    private readonly List<(Func<byte[], bool> Match, Func<byte[], bool> Answer)> _isSvgRules = [];

    /// <summary>Makes every later <c>IsSvg</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIsSvg(Func<byte[], bool> answer) => AnswerIsSvg((_) => true, answer);

    /// <summary>Makes a later <c>IsSvg</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIsSvg(Func<byte[], bool> match, Func<byte[], bool> answer)
    {
        lock (_isSvgRules) _isSvgRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public bool IsSvg(byte[] bytes)
    {
        lock (IsSvgCalls) IsSvgCalls.Add(bytes);
        Func<byte[], bool>? answer = null;
        lock (_isSvgRules) answer = _isSvgRules.LastOrDefault(rule => rule.Match(bytes)).Answer;
        if ((answer ?? OnIsSvg) is { } on)
        {
            return on(bytes);
        }

        return Strict ? throw new InvalidOperationException("IsSvg was called without an answer on a strict fake.") : default;
    }

    /// <summary>What <c>StripMetadata</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<byte[], StrippedImage>? OnStripMetadata { get; set; }

    /// <summary>The arguments of every <c>StripMetadata</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<byte[]> StripMetadataCalls { get; } = [];

    /// <summary>The rules <c>AnswerStripMetadata</c> added, newest last.</summary>
    private readonly List<(Func<byte[], bool> Match, Func<byte[], StrippedImage> Answer)> _stripMetadataRules = [];

    /// <summary>Makes every later <c>StripMetadata</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerStripMetadata(Func<byte[], StrippedImage> answer) => AnswerStripMetadata((_) => true, answer);

    /// <summary>Makes a later <c>StripMetadata</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerStripMetadata(Func<byte[], bool> match, Func<byte[], StrippedImage> answer)
    {
        lock (_stripMetadataRules) _stripMetadataRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public StrippedImage StripMetadata(byte[] bytes)
    {
        lock (StripMetadataCalls) StripMetadataCalls.Add(bytes);
        Func<byte[], StrippedImage>? answer = null;
        lock (_stripMetadataRules) answer = _stripMetadataRules.LastOrDefault(rule => rule.Match(bytes)).Answer;
        if ((answer ?? OnStripMetadata) is { } on)
        {
            return on(bytes);
        }

        return Strict ? throw new InvalidOperationException("StripMetadata was called without an answer on a strict fake.") : default!;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => IsValidImageCalls.Count + IsSvgCalls.Count + StripMetadataCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (IsValidImageCalls) IsValidImageCalls.Clear();
        lock (IsSvgCalls) IsSvgCalls.Clear();
        lock (StripMetadataCalls) StripMetadataCalls.Clear();
    }
}
