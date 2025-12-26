using System;
using System.Collections.Generic;

namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// Income
/// </summary>
public partial class Income
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
    /// DepositMyAssetProductName
    /// </summary>
    public string? DepositMyAssetProductName { get; set; }

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

    public virtual Asset Asset { get; set; } = new Asset();
}
