namespace Maroik.Website.Models.ViewModels.Dashboards;

/// <summary>What the page's script draws on one chart canvas; property names are read by the script as they are.</summary>
public class DashboardChartViewModel
{
    /// <summary>The id of the canvas it is drawn on.</summary>
    public string CanvasId { get; set; } = "";

    /// <summary>The sub-class of each slice (the resource key of its label).</summary>
    public List<string> SubClasses { get; set; } = [];

    /// <summary>The amount of each slice.</summary>
    public List<decimal> Amounts { get; set; } = [];

    /// <summary>The share of each slice, 0-100.</summary>
    public List<double> Percentages { get; set; } = [];
}
