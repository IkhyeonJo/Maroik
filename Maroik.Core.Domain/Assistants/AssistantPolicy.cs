namespace Maroik.Core.Domain.Assistants;

/// <summary>
/// The limits of the AI chat. A local model on the server's CPU reads every token of the prompt before it answers, so what reaches it
/// is kept small: a short question, a few earlier turns, a bounded number of rows per tool and of tool rounds per answer.
/// </summary>
public static class AssistantPolicy
{
    /// <summary>The longest question, in characters.</summary>
    public const int MaxQuestionLength = 500;

    /// <summary>How many earlier turns (user and assistant) of the chat the model sees with a new question.</summary>
    public const int MaxHistoryMessages = 8;

    /// <summary>The longest earlier turn the model sees, in characters; a longer one is cut.</summary>
    public const int MaxHistoryMessageLength = 2000;

    /// <summary>
    /// How many times the model may ask for tools before it must answer. A question needs one or two rounds; the bound stops a model
    /// that keeps calling tools from holding the CPU.
    /// </summary>
    public const int MaxToolRounds = 4;

    /// <summary>How many records one tool answer lists. The totals always cover every record of the period.</summary>
    public const int MaxToolRows = 30;

    /// <summary>The longest period a tool reads, in days, both ends included (a leap year fits).</summary>
    public const int MaxPeriodDays = 366;
}
