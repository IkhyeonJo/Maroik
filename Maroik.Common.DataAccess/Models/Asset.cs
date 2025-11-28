using System;
using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Asset
/// </summary>
public partial class Asset
{
    /// <summary>
    /// ProductName
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// AccountEmail (ID)
    /// </summary>
    public string AccountEmail { get; set; }

    /// <summary>
    /// Item
    /// </summary>
    public string Item { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// MonetaryUnit (KRW, USD, ETC)
    /// </summary>
    public string MonetaryUnit { get; set; }

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
    public string Note { get; set; }

    /// <summary>
    /// Deleted
    /// </summary>
    public bool Deleted { get; set; }

    public virtual Account AccountEmailNavigation { get; set; }

    public virtual ICollection<Expenditure> Expenditures { get; set; } = new List<Expenditure>();

    public virtual ICollection<FixedExpenditure> FixedExpenditures { get; set; } = new List<FixedExpenditure>();

    public virtual ICollection<FixedIncome> FixedIncomes { get; set; } = new List<FixedIncome>();

    public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();
}
