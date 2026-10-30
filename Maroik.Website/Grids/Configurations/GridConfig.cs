namespace Maroik.Website.Grids.Configurations;

public class GridConfig
{
    public String Name { get; set; }
    public GridColumnConfig[] Columns { get; set; }

    public GridConfig()
    {
        Name = "";
        Columns = Array.Empty<GridColumnConfig>();
    }
}
