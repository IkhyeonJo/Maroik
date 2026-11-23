namespace Maroik.Core.Domain.Assistants;

/// <summary>
/// The earlier turns a model sees with a new question. They come from the browser (the chat is not stored), so only user and assistant
/// turns are kept — a forged "system" or "tool" turn could otherwise give the model new rules or fake data — and only the last
/// <see cref="AssistantPolicy.MaxHistoryMessages"/>, each cut to <see cref="AssistantPolicy.MaxHistoryMessageLength"/> characters.
/// </summary>
public static class AssistantHistory
{
    /// <summary>The turns to send, oldest first, with the roles written as <see cref="AssistantTurn.User"/> / <see cref="AssistantTurn.Assistant"/>.</summary>
    public static IReadOnlyList<AssistantTurn> Trim(IEnumerable<AssistantTurn> turns)
    {
        List<AssistantTurn> kept = [];
        foreach (AssistantTurn turn in turns)
        {
            string? role = turn.Role.Trim().ToLowerInvariant() switch
            {
                AssistantTurn.User => AssistantTurn.User,
                AssistantTurn.Assistant => AssistantTurn.Assistant,
                _ => null,
            };
            if (role is null || string.IsNullOrWhiteSpace(turn.Content))
                continue;

            string content = turn.Content.Length > AssistantPolicy.MaxHistoryMessageLength
                ? turn.Content[..AssistantPolicy.MaxHistoryMessageLength]
                : turn.Content;
            kept.Add(new AssistantTurn(role, content));
        }

        return kept.Count > AssistantPolicy.MaxHistoryMessages ? kept[^AssistantPolicy.MaxHistoryMessages..] : kept;
    }
}
