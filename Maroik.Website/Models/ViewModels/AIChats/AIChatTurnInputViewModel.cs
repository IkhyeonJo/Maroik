namespace Maroik.Website.Models.ViewModels.AIChats;

/// <summary>An earlier turn of the chat, as the page sends it back with a new question.</summary>
public class AIChatTurnInputViewModel
{
    /// <summary><c>user</c> or <c>assistant</c>; the server drops any other role.</summary>
    public string? Role { get; set; }

    /// <summary>What was said.</summary>
    public string? Content { get; set; }
}
