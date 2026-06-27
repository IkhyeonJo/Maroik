using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Finance;

/// <summary>
/// Aggregate root for a recurring fixed income (e.g. monthly salary, pension).
/// Credited to an asset on a specific month/day each cycle until the <see cref="MaturityDate"/>.
/// </summary>
public sealed class FixedIncome : AggregateRoot<long>
{
    /// <summary>Email of the owning account.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Top-level income category (e.g. "RegularIncome").</summary>
    public string MainClass { get; private set; }

    /// <summary>Detailed income sub-category (e.g. "LaborIncome", "PensionIncome").</summary>
    public string SubClass { get; private set; }

    /// <summary>Description of the recurring income source (e.g. "Monthly salary").</summary>
    public string? Content { get; private set; }

    /// <summary>Expected income amount per cycle.</summary>
    public Money Amount { get; private set; }

    /// <summary>Asset product name where the income is deposited (reference by identity).</summary>
    public string DepositMyAssetProductName { get; private set; }

    /// <summary>Month of the income cycle (1–12; 0 means every month).</summary>
    public short DepositMonth { get; private set; }

    /// <summary>Day of the month the income is credited.</summary>
    public short DepositDay { get; private set; }

    /// <summary>Date when the recurring income ends (e.g. contract end date).</summary>
    public DateTime MaturityDate { get; private set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; private set; }

    /// <summary>True when the income was not received on the scheduled day.</summary>
    public bool Unpunctuality { get; private set; }

    private FixedIncome(
        long id,
        Email accountEmail,
        string mainClass,
        string subClass,
        string? content,
        Money amount,
        string depositMyAssetProductName,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note) : base(id)
    {
        AccountEmail = accountEmail;
        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = amount;
        DepositMyAssetProductName = depositMyAssetProductName;
        DepositMonth = depositMonth;
        DepositDay = depositDay;
        MaturityDate = maturityDate;
        Note = note;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds a <see cref="FixedIncome"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// Does NOT raise domain events.
    /// </summary>
    public static FixedIncome Reconstitute(
        long id,
        string accountEmail,
        string mainClass,
        string subClass,
        string? content,
        decimal amount,
        string monetaryUnit,
        string depositMyAssetProductName,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note,
        bool unpunctuality,
        DateTime created,
        DateTime updated)
    {
        var fi = new FixedIncome(
            id,
            Email.FromTrustedSource(accountEmail),
            mainClass,
            subClass,
            content,
            Money.FromTrustedSource(amount, monetaryUnit),
            depositMyAssetProductName,
            depositMonth,
            depositDay,
            maturityDate,
            note);

        fi.Unpunctuality = unpunctuality;
        fi.Created = created;
        fi.Updated = updated;
        return fi;
    }

    /// <summary>
    /// Registers a new fixed income and raises <see cref="FixedIncomeRegisteredEvent"/>.
    /// </summary>
    public static ErrorOr<FixedIncome> Register(
        string? accountEmailValue,
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("FixedIncome.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("FixedIncome.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("FixedIncome.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        if (depositDay is < 1 or > 31)
            return Error.Validation("FixedIncome.InvalidDepositDay", "Deposit day must be between 1 and 31.");

        if (depositMonth is < 0 or > 12)
            return Error.Validation("FixedIncome.InvalidDepositMonth", "Deposit month must be between 0 (every month) and 12.");

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        var fixedIncome = new FixedIncome(0, emailResult.Value, mainClass, subClass, content,
            moneyResult.Value, depositMyAssetProductName, depositMonth, depositDay, maturityDate, note);

        return fixedIncome;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Marks this income as not received on the scheduled day.</summary>
    public void MarkUnpunctual()
    {
        Unpunctuality = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Clears the unpunctuality flag when the income is eventually received.</summary>
    public void ClearUnpunctuality()
    {
        Unpunctuality = false;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Updates all editable fields.</summary>
    public ErrorOr<Success> Update(
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("FixedIncome.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("FixedIncome.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("FixedIncome.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        if (depositDay is < 1 or > 31)
            return Error.Validation("FixedIncome.InvalidDepositDay", "Deposit day must be between 1 and 31.");

        if (depositMonth is < 0 or > 12)
            return Error.Validation("FixedIncome.InvalidDepositMonth", "Deposit month must be between 0 and 12.");

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = moneyResult.Value;
        DepositMyAssetProductName = depositMyAssetProductName;
        DepositMonth = depositMonth;
        DepositDay = depositDay;
        MaturityDate = maturityDate;
        Note = note;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
