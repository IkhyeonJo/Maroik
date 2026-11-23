using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// The AI chat: answers a user's question about their own records. The model reads the records only through read-only tools the
/// server runs for the signed-in account; every number it shows comes from those tools.
/// </summary>
public interface IAIChatService
{
    /// <summary>Whether the account may use the AI chat (an administrator allows it).</summary>
    Task<bool> IsAllowedAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>
    /// Streams the answer to <paramref name="question"/>, with <paramref name="history"/> (the chat so far, from the browser) as context.
    /// A refusal or failure is the last event, of type <see cref="Enums.AIChatEventType.Error"/>; nothing is thrown.
    /// </summary>
    IAsyncEnumerable<AIChatEvent> AskAsync(
        string accountEmail, string timeZoneIanaId, string? question, IReadOnlyList<AIChatTurnRequest> history, CancellationToken ct = default);
}
