using Maroik.Website.Grids.Configurations;

namespace Maroik.Website.Tests.Grids.Configurations;

public class GridColumnConfigTests
{
    [Fact]
    public void GridColumnConfig_Defaults()
    {
        GridColumnConfig actual = new();

        Assert.False(actual.Hidden);
        Assert.Empty(actual.Width);
        Assert.Empty(actual.Name);
    }
}
