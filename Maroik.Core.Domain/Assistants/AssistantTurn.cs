namespace Maroik.Core.Domain.Assistants;

/// <summary>One earlier turn of a chat.</summary>
/// <param name="Role"><see cref="User"/> or <see cref="Assistant"/>.</param>
/// <param name="Content">What was said.</param>
public sealed record AssistantTurn(string Role, string Content)
{
    /// <summary>The role of a turn the user wrote.</summary>
    public const string User = "user";

    /// <summary>The role of a turn the model answered.</summary>
    public const string Assistant = "assistant";
}
