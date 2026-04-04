namespace Maroik.Core.Contract.Dtos;

public class AssetRequest
{
    public string? ProductName { get; set; }
    public string? AccountEmail { get; set; }
    public string? Item { get; set; }
    public decimal Amount { get; set; }
    public string? MonetaryUnit { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string? Note { get; set; }
    public bool Deleted { get; set; }
}