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
}
