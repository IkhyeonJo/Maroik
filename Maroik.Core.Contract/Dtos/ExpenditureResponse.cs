namespace Maroik.Core.Contract.Dtos;

public class ExpenditureResponse
{
    public long Id { get; set; }
    public string? AccountEmail { get; set; }
    public string? MainClass { get; set; }
    public string? SubClass { get; set; }
    public string? Content { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? MyDepositAsset { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string? Note { get; set; }
}