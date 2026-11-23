namespace Maroik.Core.Contract.Enums;

/// <summary>What an <see cref="Dtos.AIChatEvent"/> carries.</summary>
public enum AIChatEventType
{
    /// <summary>The next piece of the answer's text.</summary>
    Text,

    /// <summary>The model is reading the account's data with a tool (shown as "looking up ..." while it waits).</summary>
    Tool,

    /// <summary>The answer failed; the chat ends here.</summary>
    Error,
}
