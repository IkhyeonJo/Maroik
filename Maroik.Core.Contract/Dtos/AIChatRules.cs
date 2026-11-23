namespace Maroik.Core.Contract.Dtos;

/// <summary>The AI chat's limits the page mirrors for its form (the server checks them again).</summary>
/// <param name="MaxQuestionLength">The longest question, in characters.</param>
public sealed record AIChatRules(int MaxQuestionLength);
