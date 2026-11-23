using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Dtos;

/// <summary>One step of the AI chat's streamed answer.</summary>
public sealed record AIChatEvent
{
    /// <summary>What this step carries.</summary>
    public required AIChatEventType Type { get; init; }

    /// <summary>For <see cref="AIChatEventType.Text"/>: the next piece of the answer.</summary>
    public string Text { get; init; } = "";

    /// <summary>For <see cref="AIChatEventType.Tool"/>: the name of the tool being run.</summary>
    public string ToolName { get; init; } = "";

    /// <summary>For <see cref="AIChatEventType.Error"/>: the message key (a <see cref="ServiceErrorKeys"/> value or a domain template).</summary>
    public string ErrorKey { get; init; } = "";

    /// <summary>For <see cref="AIChatEventType.Error"/>: the values of the message key's placeholders.</summary>
    public object[] ErrorArgs { get; init; } = [];

    /// <summary>A piece of the answer.</summary>
    public static AIChatEvent OfText(string text) => new() { Type = AIChatEventType.Text, Text = text };

    /// <summary>A tool being run.</summary>
    public static AIChatEvent OfTool(string toolName) => new() { Type = AIChatEventType.Tool, ToolName = toolName };

    /// <summary>A failure.</summary>
    public static AIChatEvent OfError(string errorKey, params object[] errorArgs) =>
        new() { Type = AIChatEventType.Error, ErrorKey = errorKey, ErrorArgs = errorArgs };
}
