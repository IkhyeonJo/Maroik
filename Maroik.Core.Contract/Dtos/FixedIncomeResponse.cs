namespace Maroik.Core.Contract.Dtos;

public class FixedIncomeResponse
{
    public long Id { get; set; }
    public string? AccountEmail { get; set; }
    public string? MainClass { get; set; }
    public string? SubClass { get; set; }
    public string? Content { get; set; }
    public decimal Amount { get; set; }
    public string? DepositMyAssetProductName { get; set; }
    public short DepositMonth { get; set; }
    public short DepositDay { get; set; }
    public DateTime MaturityDate { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string? Note { get; set; }
    public bool Unpunctuality { get; set; }
}