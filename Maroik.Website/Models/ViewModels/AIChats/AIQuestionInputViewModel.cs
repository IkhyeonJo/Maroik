namespace Maroik.Website.Models.ViewModels.AIChats;

/// <summary>A question to the AI chat with the chat so far (the page keeps the chat; the server stores nothing).</summary>
public class AIQuestionInputViewModel
{
    /// <summary>The question. Its rules (not blank, the longest length) are the domain's, checked by the service.</summary>
    public string? Question { get; set; }

    /// <summary>The earlier turns, oldest first; the server keeps only the most recent ones.</summary>
    public List<AIChatTurnInputViewModel> History { get; set; } = [];
}
