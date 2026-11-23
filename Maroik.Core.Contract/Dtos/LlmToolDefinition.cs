namespace Maroik.Core.Contract.Dtos;

/// <summary>A tool a model may call (function calling).</summary>
/// <param name="Name">The name the model calls it by.</param>
/// <param name="Description">What it answers, for the model.</param>
/// <param name="ParametersJsonSchema">Its arguments as a JSON Schema object.</param>
public sealed record LlmToolDefinition(string Name, string Description, string ParametersJsonSchema);
