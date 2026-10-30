using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAttachmentContentService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAttachmentContentService : IAttachmentContentService
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>ValidateAttachedFile</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AttachedFileDto?, ServiceResult?>? OnValidateAttachedFile { get; set; }

    /// <summary>The arguments of every <c>ValidateAttachedFile</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<AttachedFileDto?> ValidateAttachedFileCalls { get; } = [];

    /// <summary>The rules <c>AnswerValidateAttachedFile</c> added, newest last.</summary>
    private readonly List<(Func<AttachedFileDto?, bool> Match, Func<AttachedFileDto?, ServiceResult?> Answer)> _validateAttachedFileRules = [];

    /// <summary>Makes every later <c>ValidateAttachedFile</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerValidateAttachedFile(Func<AttachedFileDto?, ServiceResult?> answer) => AnswerValidateAttachedFile((_) => true, answer);

    /// <summary>Makes a later <c>ValidateAttachedFile</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerValidateAttachedFile(Func<AttachedFileDto?, bool> match, Func<AttachedFileDto?, ServiceResult?> answer)
    {
        lock (_validateAttachedFileRules) _validateAttachedFileRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public ServiceResult? ValidateAttachedFile(AttachedFileDto? attachedFile)
    {
        lock (ValidateAttachedFileCalls) ValidateAttachedFileCalls.Add(attachedFile);
        Func<AttachedFileDto?, ServiceResult?>? answer = null;
        lock (_validateAttachedFileRules) answer = _validateAttachedFileRules.LastOrDefault(rule => rule.Match(attachedFile)).Answer;
        if ((answer ?? OnValidateAttachedFile) is { } on)
        {
            return on(attachedFile);
        }

        return Strict ? throw new InvalidOperationException("ValidateAttachedFile was called without an answer on a strict fake.") : default;
    }

    /// <summary>What <c>UploadSummernoteImageAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AttachedFileDto, string, string, string, CancellationToken, Task<SummernoteUploadResult>>? OnUploadSummernoteImageAsync { get; set; }

    /// <summary>The arguments of every <c>UploadSummernoteImageAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(AttachedFileDto File, string Area, string SubArea, string ActorEmail, CancellationToken Ct)> UploadSummernoteImageAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUploadSummernoteImageAsync</c> added, newest last.</summary>
    private readonly List<(Func<AttachedFileDto, string, string, string, CancellationToken, bool> Match, Func<AttachedFileDto, string, string, string, CancellationToken, Task<SummernoteUploadResult>> Answer)> _uploadSummernoteImageAsyncRules = [];

    /// <summary>Makes every later <c>UploadSummernoteImageAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadSummernoteImageAsync(Func<AttachedFileDto, string, string, string, CancellationToken, Task<SummernoteUploadResult>> answer) => AnswerUploadSummernoteImageAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UploadSummernoteImageAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadSummernoteImageAsync(Func<AttachedFileDto, string, string, string, CancellationToken, bool> match, Func<AttachedFileDto, string, string, string, CancellationToken, Task<SummernoteUploadResult>> answer)
    {
        lock (_uploadSummernoteImageAsyncRules) _uploadSummernoteImageAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileDto file, string area, string subArea, string actorEmail, CancellationToken ct)
    {
        lock (UploadSummernoteImageAsyncCalls) UploadSummernoteImageAsyncCalls.Add((file, area, subArea, actorEmail, ct));
        Func<AttachedFileDto, string, string, string, CancellationToken, Task<SummernoteUploadResult>>? answer = null;
        lock (_uploadSummernoteImageAsyncRules) answer = _uploadSummernoteImageAsyncRules.LastOrDefault(rule => rule.Match(file, area, subArea, actorEmail, ct)).Answer;
        if ((answer ?? OnUploadSummernoteImageAsync) is { } on)
        {
            return on(file, area, subArea, actorEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("UploadSummernoteImageAsync was called without an answer on a strict fake.") : Task.FromResult<SummernoteUploadResult>(default!);
    }

    /// <summary>What <c>OpenFileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<Stream?>>? OnOpenFileAsync { get; set; }

    /// <summary>The arguments of every <c>OpenFileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string FilePath, CancellationToken Ct)> OpenFileAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerOpenFileAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<Stream?>> Answer)> _openFileAsyncRules = [];

    /// <summary>Makes every later <c>OpenFileAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerOpenFileAsync(Func<string, CancellationToken, Task<Stream?>> answer) => AnswerOpenFileAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>OpenFileAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerOpenFileAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<Stream?>> answer)
    {
        lock (_openFileAsyncRules) _openFileAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Stream?> OpenFileAsync(string filePath, CancellationToken ct)
    {
        lock (OpenFileAsyncCalls) OpenFileAsyncCalls.Add((filePath, ct));
        Func<string, CancellationToken, Task<Stream?>>? answer = null;
        lock (_openFileAsyncRules) answer = _openFileAsyncRules.LastOrDefault(rule => rule.Match(filePath, ct)).Answer;
        if ((answer ?? OnOpenFileAsync) is { } on)
        {
            return on(filePath, ct);
        }

        return Strict ? throw new InvalidOperationException("OpenFileAsync was called without an answer on a strict fake.") : Task.FromResult<Stream?>(default);
    }

    /// <summary>What <c>PrepareHtmlForDisplayAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<(string, bool)>>? OnPrepareHtmlForDisplayAsync { get; set; }

    /// <summary>The arguments of every <c>PrepareHtmlForDisplayAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Html, CancellationToken Ct)> PrepareHtmlForDisplayAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerPrepareHtmlForDisplayAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<(string, bool)>> Answer)> _prepareHtmlForDisplayAsyncRules = [];

    /// <summary>Makes every later <c>PrepareHtmlForDisplayAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerPrepareHtmlForDisplayAsync(Func<string, CancellationToken, Task<(string, bool)>> answer) => AnswerPrepareHtmlForDisplayAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>PrepareHtmlForDisplayAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerPrepareHtmlForDisplayAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<(string, bool)>> answer)
    {
        lock (_prepareHtmlForDisplayAsyncRules) _prepareHtmlForDisplayAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<(string, bool)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct)
    {
        lock (PrepareHtmlForDisplayAsyncCalls) PrepareHtmlForDisplayAsyncCalls.Add((html, ct));
        Func<string, CancellationToken, Task<(string, bool)>>? answer = null;
        lock (_prepareHtmlForDisplayAsyncRules) answer = _prepareHtmlForDisplayAsyncRules.LastOrDefault(rule => rule.Match(html, ct)).Answer;
        if ((answer ?? OnPrepareHtmlForDisplayAsync) is { } on)
        {
            return on(html, ct);
        }

        return Strict ? throw new InvalidOperationException("PrepareHtmlForDisplayAsync was called without an answer on a strict fake.") : Task.FromResult<(string, bool)>(default);
    }

    /// <summary>What <c>SanitizeAndDecryptContent</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnSanitizeAndDecryptContent { get; set; }

    /// <summary>The arguments of every <c>SanitizeAndDecryptContent</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> SanitizeAndDecryptContentCalls { get; } = [];

    /// <summary>The rules <c>AnswerSanitizeAndDecryptContent</c> added, newest last.</summary>
    private readonly List<(Func<string, bool> Match, Func<string, string> Answer)> _sanitizeAndDecryptContentRules = [];

    /// <summary>Makes every later <c>SanitizeAndDecryptContent</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitizeAndDecryptContent(Func<string, string> answer) => AnswerSanitizeAndDecryptContent((_) => true, answer);

    /// <summary>Makes a later <c>SanitizeAndDecryptContent</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitizeAndDecryptContent(Func<string, bool> match, Func<string, string> answer)
    {
        lock (_sanitizeAndDecryptContentRules) _sanitizeAndDecryptContentRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string SanitizeAndDecryptContent(string html)
    {
        lock (SanitizeAndDecryptContentCalls) SanitizeAndDecryptContentCalls.Add(html);
        Func<string, string>? answer = null;
        lock (_sanitizeAndDecryptContentRules) answer = _sanitizeAndDecryptContentRules.LastOrDefault(rule => rule.Match(html)).Answer;
        if ((answer ?? OnSanitizeAndDecryptContent) is { } on)
        {
            return on(html);
        }

        return Strict ? throw new InvalidOperationException("SanitizeAndDecryptContent was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>SanitizeContent</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string>? OnSanitizeContent { get; set; }

    /// <summary>The arguments of every <c>SanitizeContent</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> SanitizeContentCalls { get; } = [];

    /// <summary>The rules <c>AnswerSanitizeContent</c> added, newest last.</summary>
    private readonly List<(Func<string, bool> Match, Func<string, string> Answer)> _sanitizeContentRules = [];

    /// <summary>Makes every later <c>SanitizeContent</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitizeContent(Func<string, string> answer) => AnswerSanitizeContent((_) => true, answer);

    /// <summary>Makes a later <c>SanitizeContent</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSanitizeContent(Func<string, bool> match, Func<string, string> answer)
    {
        lock (_sanitizeContentRules) _sanitizeContentRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string SanitizeContent(string html)
    {
        lock (SanitizeContentCalls) SanitizeContentCalls.Add(html);
        Func<string, string>? answer = null;
        lock (_sanitizeContentRules) answer = _sanitizeContentRules.LastOrDefault(rule => rule.Match(html)).Answer;
        if ((answer ?? OnSanitizeContent) is { } on)
        {
            return on(html);
        }

        return Strict ? throw new InvalidOperationException("SanitizeContent was called without an answer on a strict fake.") : default!;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => ValidateAttachedFileCalls.Count + UploadSummernoteImageAsyncCalls.Count + OpenFileAsyncCalls.Count + PrepareHtmlForDisplayAsyncCalls.Count + SanitizeAndDecryptContentCalls.Count + SanitizeContentCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (ValidateAttachedFileCalls) ValidateAttachedFileCalls.Clear();
        lock (UploadSummernoteImageAsyncCalls) UploadSummernoteImageAsyncCalls.Clear();
        lock (OpenFileAsyncCalls) OpenFileAsyncCalls.Clear();
        lock (PrepareHtmlForDisplayAsyncCalls) PrepareHtmlForDisplayAsyncCalls.Clear();
        lock (SanitizeAndDecryptContentCalls) SanitizeAndDecryptContentCalls.Clear();
        lock (SanitizeContentCalls) SanitizeContentCalls.Clear();
    }
}
