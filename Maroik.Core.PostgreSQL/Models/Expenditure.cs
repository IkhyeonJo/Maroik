using System;
using System.Collections.Generic;

namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// Expenditure
/// </summary>
public partial class Expenditure
{
    /// <summary>
    /// PK
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Account Email (ID)
    /// </summary>
    public string? AccountEmail { get; set; }

    /// <summary>
    /// MainClass
    /// </summary>
    public string? MainClass { get; set; }

    /// <summary>
    /// SubClass
    /// </summary>
    public string? SubClass { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// PaymentMethod
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// MyDepositAsset
    /// </summary>
    public string? MyDepositAsset { get; set; }

    /// <summary>
    /// Created
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Updated
    /// </summary>
    public DateTime Updated { get; set; }

    /// <summary>
    /// Note
    /// </summary>
    public string? Note { get; set; }

    public virtual Asset? Asset { get; set; }
}
