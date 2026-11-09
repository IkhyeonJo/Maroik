using Maroik.Core.Domain.FileManagers;

namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>How full a drive counts as.</summary>
public class FileManagerUsedPercentTests
{
    /// <summary>A share of the quota, rounded down and capped at 100; with no quota, anything stored is full.</summary>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 100)]
    [InlineData(0, 100, 0)]
    [InlineData(999, 1000, 99)]
    [InlineData(150, 100, 100)]
    [InlineData(long.MaxValue, 1, 100)]
    public void UsedPercent_IsTheShareOfTheQuota(long used, long quota, int expected)
    {
        Assert.Equal(expected, FileManagerTree.UsedPercent(used, quota));
    }
}
