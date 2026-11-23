namespace Maroik.Core.Contract.Dtos;

/// <summary>An earlier turn of the chat, as the browser sends it back (the chat is not stored on the server).</summary>
public sealed class AIChatTurnRequest
{
    /// <summary><c>user</c> or <c>assistant</c>; any other role is dropped.</summary>
    public string? Role { get; set; }

    /// <summary>What was said.</summary>
    public string? Content { get; set; }
}
