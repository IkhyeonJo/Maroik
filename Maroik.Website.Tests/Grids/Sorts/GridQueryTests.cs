using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Tests.Grids.Sorts;

public class GridQueryTests
{
    [Fact]
    public void IsOrdered_False()
    {
        Assert.False(GridQuery.IsOrdered(Array.Empty<Object>().AsQueryable().Where(_ => true)));
    }

    [Fact]
    public void IsOrdered_Ascending_True()
    {
        Assert.True(GridQuery.IsOrdered(Array.Empty<Object>().AsQueryable().OrderBy(_ => 0)));
    }

    [Fact]
    public void IsOrdered_Descending_True()
    {
        Assert.True(GridQuery.IsOrdered(Array.Empty<Object>().AsQueryable().OrderByDescending(_ => 0)));
    }
}
