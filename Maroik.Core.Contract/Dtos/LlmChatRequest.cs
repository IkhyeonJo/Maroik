namespace Maroik.Core.Contract.Dtos;

/// <summary>One request to a language model: the chat so far and the tools it may call.</summary>
/// <param name="Messages">The chat so far, oldest first.</param>
/// <param name="Tools">The tools the model may call.</param>
public sealed record LlmChatRequest(IReadOnlyList<LlmMessage> Messages, IReadOnlyList<LlmToolDefinition> Tools);
