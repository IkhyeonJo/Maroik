using System.Text.RegularExpressions;
using Maroik.Website.Extensions;

namespace Maroik.Website.Tests.Extensions;

/// <summary>Unit tests for <see cref="ExportExcelExtensions"/>.</summary>
public class ExportExcelExtensionsTests
{
    [Fact]
    public void ToExcelFileName_StartsWithPrefixAndEndsWithXlsxExtension()
    {
        string fileName = "AccountBook".ToExcelFileName("UTC");

        Assert.StartsWith("AccountBook-", fileName);
        Assert.EndsWith(".xlsx", fileName);
    }

    [Fact]
    public void ToExcelFileName_MatchesTimestampPattern()
    {
        string fileName = "Menu".ToExcelFileName("UTC");

        Assert.Matches(new Regex(@"^Menu-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{3}\.xlsx$"), fileName);
    }
}
