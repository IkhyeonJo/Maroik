using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance;

/// <summary>
/// Aggregate root representing a single one-time income record.
/// The deposit-asset reference points to an <see cref="Asset"/> identity (ProductName)
/// rather than an object reference, preserving aggregate boundaries.
/// </summary>
public sealed class Income : AggregateRoot<long>
{
    /// <summary>Email of the account that received the income.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Top-level income category (e.g. "RegularIncome", "IrregularIncome").</summary>
    public string MainClass { get; private set; }

    /// <summary>Detailed income sub-category (e.g. "LaborIncome", "FinancialIncome").</summary>
    public string SubClass { get; private set; }

    /// <summary>Description of the income source.</summary>
    public string? Content { get; private set; }

    /// <summary>Income amount received.</summary>
    public Money Amount { get; private set; }

    /// <summary>Asset product name where the income was deposited (reference by identity).</summary>
    public string DepositMyAssetProductName { get; private set; }

    /// <summary>UTC timestamp when the income was recorded.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent edit.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; private set; }

    private Income(
        long id,
        Email accountEmail,
        string mainClass,
        string subClass,
        string? content,
        Money amount,
        string depositMyAssetProductName,
        string? note) : base(id)
    {
        AccountEmail = accountEmail;
        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = amount;
        DepositMyAssetProductName = depositMyAssetProductName;
        Note = note;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Factory / Reconstitution
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds an <see cref="Income"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// Does NOT raise domain events.
    /// </summary>
    public static Income Reconstitute(
        long id,
        string accountEmail,
        string mainClass,
        string subClass,
        string? content,
        decimal amount,
        string monetaryUnit,
        string depositMyAssetProductName,
        string? note,
        DateTime created,
        DateTime updated)
    {
        var income = new Income(
            id,
            Email.FromTrustedSource(accountEmail),
            mainClass,
            subClass,
            content,
            Money.FromTrustedSource(amount, monetaryUnit),
            depositMyAssetProductName,
            note);

        income.Created = created;
        income.Updated = updated;
        return income;
    }

    /// <summary>
    /// Records a new income entry and raises <see cref="IncomeRecordedEvent"/>.
    /// Pass <paramref name="id"/> as 0 for new records (the repository assigns the real ID).
    /// </summary>
    public static ErrorOr<Income> Record(
        long id,
        string? accountEmailValue,
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("Income.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("Income.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("Income.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        var income = new Income(id, emailResult.Value, mainClass, subClass, content,
            moneyResult.Value, depositMyAssetProductName, note);

        return income;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Domain behaviours
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Updates all editable fields of this income record.</summary>
    public ErrorOr<Success> Update(
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("Income.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("Income.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("Income.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = moneyResult.Value;
        DepositMyAssetProductName = depositMyAssetProductName;
        Note = note;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
