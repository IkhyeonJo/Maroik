namespace Maroik.Core.Contract.Dtos;

/// <summary>One message of a chat with a language model.</summary>
/// <param name="Role"><c>system</c>, <c>user</c>, <c>assistant</c> or <c>tool</c>.</param>
/// <param name="Content">The text; for a <c>tool</c> message, the tool's JSON answer.</param>
/// <param name="ToolCalls">The tools an <c>assistant</c> message asked for (empty otherwise).</param>
/// <param name="ToolName">The tool a <c>tool</c> message answers (null otherwise).</param>
public sealed record LlmMessage(string Role, string Content, IReadOnlyList<LlmToolCall> ToolCalls, string? ToolName = null)
{
    /// <summary>A message without tool calls.</summary>
    public LlmMessage(string role, string content) : this(role, content, []) { }
}
