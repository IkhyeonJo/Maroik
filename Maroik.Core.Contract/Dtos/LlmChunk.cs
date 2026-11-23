namespace Maroik.Core.Contract.Dtos;

/// <summary>A piece of a model's streamed answer: some text, the tools it asks for, or both.</summary>
/// <param name="Content">The next piece of the answer's text (may be empty).</param>
/// <param name="ToolCalls">The tools the model asks for in this piece (usually empty).</param>
public sealed record LlmChunk(string Content, IReadOnlyList<LlmToolCall> ToolCalls);
