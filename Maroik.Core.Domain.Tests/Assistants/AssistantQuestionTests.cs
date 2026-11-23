using Maroik.Core.Domain.Assistants;

namespace Maroik.Core.Domain.Tests.Assistants;

/// <summary>What a user may ask the AI chat.</summary>
public class AssistantQuestionTests
{
    /// <summary>A question is kept trimmed.</summary>
    [Fact]
    public void AQuestion_IsTrimmed()
    {
        Assert.Equal("이번 달 지출은?", AssistantQuestion.Create("  이번 달 지출은?\n").Value);
    }

    /// <summary>An empty or blank question is refused.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankQuestion_IsRefused(string? text)
    {
        var result = AssistantQuestion.Create(text);

        Assert.Equal("Assistant.QuestionEmpty", result.FirstError.Code);
        Assert.Equal("Please enter a question.", result.FirstError.Metadata!["MessageTemplate"]);
    }

    /// <summary>A question may be <see cref="AssistantPolicy.MaxQuestionLength"/> characters long, not one more.</summary>
    [Fact]
    public void AQuestionLongerThanTheLimit_IsRefused()
    {
        Assert.False(AssistantQuestion.Create(new string('가', AssistantPolicy.MaxQuestionLength)).IsError);

        var result = AssistantQuestion.Create(new string('가', AssistantPolicy.MaxQuestionLength + 1));

        Assert.Equal("Assistant.QuestionTooLong", result.FirstError.Code);
        Assert.Equal("A question can be at most {0} characters long.", result.FirstError.Metadata!["MessageTemplate"]);
    }
}
