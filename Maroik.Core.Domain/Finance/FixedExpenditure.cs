using ErrorOr;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Finance;

/// <summary>
/// Aggregate root for a recurring fixed expenditure (e.g. subscription, insurance premium).
/// Charged on a specific month/day each cycle until the <see cref="MaturityDate"/>.
/// </summary>
public sealed class FixedExpenditure : AggregateRoot<long>
{
    /// <summary>Email of the owning account.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Top-level expense category.</summary>
    public string MainClass { get; private set; }

    /// <summary>Detailed expense sub-category.</summary>
    public string SubClass { get; private set; }

    /// <summary>Description of the recurring expense (e.g. "Netflix subscription").</summary>
    public string? Content { get; private set; }

    /// <summary>Recurring charge amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>Asset product name used as the payment source.</summary>
    public string PaymentMethod { get; private set; }

    /// <summary>Asset product name that is debited.</summary>
    public string? MyDepositAsset { get; private set; }

    /// <summary>Month of the billing cycle (1–12; 0 means every month).</summary>
    public short DepositMonth { get; private set; }

    /// <summary>Day of the month the charge is deducted.</summary>
    public short DepositDay { get; private set; }

    /// <summary>Date when the recurring charge ends (contract / subscription expiry).</summary>
    public DateTime MaturityDate { get; private set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Optional free-text note.</summary>
    public string? Note { get; private set; }

    /// <summary>True when the charge was not collected on the scheduled day.</summary>
    public bool Unpunctuality { get; private set; }

    private FixedExpenditure(
        long id,
        Email accountEmail,
        string mainClass,
        string subClass,
        string? content,
        Money amount,
        string paymentMethod,
        string? myDepositAsset,
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
        PaymentMethod = paymentMethod;
        MyDepositAsset = myDepositAsset;
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
    /// Rebuilds a <see cref="FixedExpenditure"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static FixedExpenditure Reconstitute(
        long id,
        string accountEmail,
        string mainClass,
        string subClass,
        string? content,
        decimal amount,
        string monetaryUnit,
        string paymentMethod,
        string? myDepositAsset,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note,
        bool unpunctuality,
        DateTime created,
        DateTime updated)
    {
        var fe = new FixedExpenditure(
            id,
            Email.FromTrustedSource(accountEmail),
            mainClass,
            subClass,
            content,
            Money.FromTrustedSource(amount, monetaryUnit),
            paymentMethod,
            myDepositAsset,
            depositMonth,
            depositDay,
            maturityDate,
            note) { Unpunctuality = unpunctuality, Created = created, Updated = updated };

        return fe;
    }

    /// <summary>
    /// Registers a new fixed expenditure.
    /// </summary>
    public static ErrorOr<FixedExpenditure> Register(
        string? accountEmailValue,
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? paymentMethod,
        string? myDepositAsset,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("FixedExpenditure.MainClassEmpty", "Main expense category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("FixedExpenditure.SubClassEmpty", "Sub expense category cannot be empty.");

        if (string.IsNullOrWhiteSpace(paymentMethod))
            return Error.Validation("FixedExpenditure.PaymentMethodEmpty", "Payment method cannot be empty.");

        if (depositDay is < 1 or > 31)
            return Error.Validation("FixedExpenditure.InvalidDepositDay", "Deposit day must be between 1 and 31.");

        if (depositMonth is < 0 or > 12)
            return Error.Validation("FixedExpenditure.InvalidDepositMonth", "Deposit month must be between 0 (every month) and 12.");

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        var fixedExpenditure = new FixedExpenditure(0, emailResult.Value, mainClass, subClass, content,
            moneyResult.Value, paymentMethod, myDepositAsset, depositMonth, depositDay, maturityDate, note);

        return fixedExpenditure;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Marks this charge as not collected on the scheduled day.</summary>
    public void MarkUnpunctual()
    {
        Unpunctuality = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Clears the unpunctuality flag when the charge is eventually collected.</summary>
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
        string? paymentMethod,
        string? myDepositAsset,
        short depositMonth,
        short depositDay,
        DateTime maturityDate,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("FixedExpenditure.MainClassEmpty", "Main expense category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("FixedExpenditure.SubClassEmpty", "Sub expense category cannot be empty.");

        if (string.IsNullOrWhiteSpace(paymentMethod))
            return Error.Validation("FixedExpenditure.PaymentMethodEmpty", "Payment method cannot be empty.");

        if (depositDay is < 1 or > 31)
            return Error.Validation("FixedExpenditure.InvalidDepositDay", "Deposit day must be between 1 and 31.");

        if (depositMonth is < 0 or > 12)
            return Error.Validation("FixedExpenditure.InvalidDepositMonth", "Deposit month must be between 0 and 12.");

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = moneyResult.Value;
        PaymentMethod = paymentMethod;
        MyDepositAsset = myDepositAsset;
        DepositMonth = depositMonth;
        DepositDay = depositDay;
        MaturityDate = maturityDate;
        Note = note;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
