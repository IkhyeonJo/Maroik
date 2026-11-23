using ErrorOr;
using Maroik.Core.Domain.Errors;

namespace Maroik.Core.Domain.Assistants;

/// <summary>A question to the AI chat: not blank, at most <see cref="AssistantPolicy.MaxQuestionLength"/> characters.</summary>
public static class AssistantQuestion
{
    /// <summary>The trimmed question, or why it is refused.</summary>
    public static ErrorOr<string> Create(string? text)
    {
        string trimmed = text?.Trim() ?? "";
        if (trimmed.Length == 0)
            return DomainError.Validation("Assistant.QuestionEmpty", "Please enter a question.");
        if (trimmed.Length > AssistantPolicy.MaxQuestionLength)
            return DomainError.Validation("Assistant.QuestionTooLong", "A question can be at most {0} characters long.", AssistantPolicy.MaxQuestionLength);
        return trimmed;
    }
}
