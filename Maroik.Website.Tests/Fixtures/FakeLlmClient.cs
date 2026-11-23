using System.Runtime.CompilerServices;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// An in-process stand-in for the Ollama server: answers each request with the next scripted answer (a list of pieces), or throws
/// <see cref="Failure"/>, and keeps every request it got.
/// </summary>
public sealed class FakeLlmClient : ILlmClient
{
    /// <summary>The scripted answers, one per request, in order.</summary>
    private readonly Queue<LlmChunk[]> _answers = new();

    /// <summary>Every request received.</summary>
    public List<LlmChatRequest> Requests { get; } = [];

    /// <summary>When set, every request throws it (an unreachable server).</summary>
    public Exception? Failure { get; set; }

    /// <summary>Adds the answer to the next request.</summary>
    public FakeLlmClient Answers(params LlmChunk[] chunks)
    {
        _answers.Enqueue(chunks);
        return this;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LlmChunk> ChatAsync(LlmChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Requests.Add(request);
        await Task.Yield();
        if (Failure is not null)
            throw Failure;
        foreach (LlmChunk chunk in _answers.Dequeue())
            yield return chunk;
    }
}
