namespace Maroik.Website.Models.ViewModels.AIChats;

/// <summary>The AI chat page (<c>AI/UserIndex</c>).</summary>
public class AIChatPageViewModel
{
    /// <summary>Whether the signed-in account may use the AI chat; when not, the page only says to ask the administrator.</summary>
    public bool Allowed { get; set; }

    /// <summary>The longest question, mirrored as the question box's <c>maxlength</c> (the server checks it again).</summary>
    public int MaxQuestionLength { get; set; }
}
