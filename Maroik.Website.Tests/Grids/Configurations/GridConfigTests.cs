using Maroik.Website.Grids.Configurations;

namespace Maroik.Website.Tests.Grids.Configurations;

public class GridConfigTests
{
    [Fact]
    public void GridConfig_Defaults()
    {
        GridConfig actual = new();

        Assert.Empty(actual.Columns);
        Assert.Empty(actual.Name);
    }
}
