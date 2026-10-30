using Maroik.Website.Grids.Rows;

namespace Maroik.Website.Tests.Grids.Rows;

public class GridRowTests
{
    [Fact]
    public void GridRow_SetsIndex()
    {
        Assert.Equal(3, new GridRow<Object>(new Object(), 3).Index);
    }

    [Fact]
    public void GridRow_SetsModel()
    {
        Object expected = new();
        Object actual = new GridRow<Object>(expected, 0).Model;

        Assert.Same(expected, actual);
    }
}
