namespace Maroik.Core.Contract.Dtos;

/// <summary>A model's request to run one tool.</summary>
/// <param name="Name">The tool's name, as the model wrote it (it may name a tool that does not exist).</param>
/// <param name="ArgumentsJson">The arguments as a JSON object, as the model wrote them (they may be malformed).</param>
public sealed record LlmToolCall(string Name, string ArgumentsJson);
