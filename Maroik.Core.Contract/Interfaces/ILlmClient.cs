using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>A language model that answers a chat and may call tools (the Ollama server on the internal network).</summary>
public interface ILlmClient
{
    /// <summary>
    /// Streams the model's answer to <paramref name="request"/> piece by piece. Throws <see cref="HttpRequestException"/> when the model
    /// server cannot be reached or refuses the request.
    /// </summary>
    IAsyncEnumerable<LlmChunk> ChatAsync(LlmChatRequest request, CancellationToken ct = default);
}
