using Maroik.FileStorage.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Maroik.FileStorage.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IFileValidationService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeFileValidationService : IFileValidationService
{
    /// <summary>What <c>ValidateAndSaveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IFormFile, string, Task<(bool, string?)>>? OnValidateAndSaveAsync { get; set; }

    /// <summary>The arguments of every <c>ValidateAndSaveAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IFormFile File, string FilePath)> ValidateAndSaveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(bool, string?)> ValidateAndSaveAsync(IFormFile file, string filePath)
    {
        lock (ValidateAndSaveAsyncCalls) ValidateAndSaveAsyncCalls.Add((file, filePath));
        if (OnValidateAndSaveAsync is { } on)
        {
            return on(file, filePath);
        }

        return Task.FromResult<(bool, string?)>(default);
    }
}
