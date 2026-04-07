namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object returned when reading a single expenditure record.
/// </summary>
public class ExpenditureResponse
{
    /// <summary>Expenditure ID (primary key).</summary>
    public long Id { get; set; }

    /// <summary>Email of the owning account.</summary>
    public string? AccountEmail { get; set; }

    /// <summary>Top-level expense category.</summary>
    public string? MainClass { get; set; }

    /// <summary>Detailed expense sub-category.</summary>
    public string? SubClass { get; set; }

    /// <summary>Description of the expenditure.</summary>
    public string? Content { get; set; }

    /// <summary>Amount spent.</summary>
    public decimal Amount { get; set; }

    /// <summary>Asset used as the payment method.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Debited asset product name.</summary>
    public string? MyDepositAsset { get; set; }

    /// <summary>UTC timestamp when the expenditure was recorded.</summary>
    public DateTime Created { get; set; }

    /// <summary>UTC last-updated timestamp.</summary>
    public DateTime Updated { get; set; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; set; }
}