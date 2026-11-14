using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="ClassSuggestionPolicy"/>, the rules shared by the income and expenditure class suggestions: which
/// content can be suggested for at all, and how content is normalized before the model reads it.
/// </summary>
public class ClassSuggestionPolicyTests
{
    /// <summary>Content without a letter or a digit (blank, punctuation only) carries nothing to suggest from.</summary>
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("...!!", false)]
    [InlineData("스타벅스", true)]
    [InlineData("(주)", true)]
    [InlineData("7", true)]
    public void HasEvidence_RequiresALetterOrADigit(string? content, bool expected)
    {
        Assert.Equal(expected, ClassSuggestionPolicy.HasEvidence(content));
    }

    /// <summary>Content is compared trimmed, with inner blanks (a tab included) collapsed to one space, lower-cased (invariant).</summary>
    [Fact]
    public void Normalize_TrimsCollapsesAndLowerCases()
    {
        Assert.Equal("netflix premium", ClassSuggestionPolicy.Normalize("  Netflix \t Premium "));
        Assert.Equal("", ClassSuggestionPolicy.Normalize(null));
    }
}
