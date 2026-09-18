using System.Text.RegularExpressions;
using Maroik.Website.Extensions;

namespace Maroik.Website.Tests.Extensions;

/// <summary>Unit tests for <see cref="ExportExcelExtensions"/>.</summary>
public class ExportExcelExtensionsTests
{
    /// <summary>To excel file name starts with prefix and ends with xlsx extension.</summary>
    [Fact]
    public void ToExcelFileName_StartsWithPrefixAndEndsWithXlsxExtension()
    {
        string fileName = "AccountBook".ToExcelFileName("UTC");

        Assert.StartsWith("AccountBook-", fileName);
        Assert.EndsWith(".xlsx", fileName);
    }

    /// <summary>To excel file name matches timestamp pattern.</summary>
    [Fact]
    public void ToExcelFileName_MatchesTimestampPattern()
    {
        string fileName = "Menu".ToExcelFileName("UTC");

        Assert.Matches(new Regex(@"^Menu-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{3}\.xlsx$"), fileName);
    }

    /// <summary>
    /// Regression test: a caller-supplied prefix containing characters invalid in a file name (it
    /// comes straight from a query string) must not throw when the framework later builds the
    /// response's Content-Disposition header from the result — those characters are stripped instead.
    /// Built from <see cref="Path.GetInvalidFileNameChars"/> itself (platform-dependent — Windows
    /// and Linux disallow different characters) rather than a hardcoded set, so this passes on both.
    /// </summary>
    [Fact]
    public void ToExcelFileName_StripsInvalidFileNameCharacters()
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string prefix = "Bad" + new string(invalidChars) + "Name";

        string fileName = prefix.ToExcelFileName("UTC");

        Assert.StartsWith("BadName-", fileName);
        foreach (char c in invalidChars)
            Assert.DoesNotContain(c, fileName);
    }
}
