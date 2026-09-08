using ErrorOr;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Finance;

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

    /// <summary>"New record" constructor: stamps <see cref="Created"/>/<see cref="Updated"/>. Used by <see cref="Record"/> only.</summary>
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

    /// <summary>Reconstitution constructor: assigns every field verbatim from trusted storage with no <see cref="DateTime.UtcNow"/> side effect.</summary>
    private Income(
        long id, Email accountEmail, string mainClass, string subClass, string? content, Money amount,
        string depositMyAssetProductName, string? note, DateTime created, DateTime updated) : base(id)
    {
        AccountEmail = accountEmail;
        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = amount;
        DepositMyAssetProductName = depositMyAssetProductName;
        Note = note;
        Created = created;
        Updated = updated;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds an <see cref="Income"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
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
        return new Income(
            id,
            Email.FromTrustedSource(accountEmail),
            mainClass,
            subClass,
            content,
            Money.FromTrustedSource(amount, monetaryUnit),
            depositMyAssetProductName,
            note,
            created,
            updated);
    }

    /// <summary>
    /// Records a new income entry.
    /// </summary>
    public static ErrorOr<Income> Record(
        string? accountEmailValue,
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        string? note = null,
        DateTime? created = null)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("Income.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("Income.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("Income.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        var textResult = FinanceTextPolicy.ValidateContentAndNote(content, note);
        if (textResult.IsError) return textResult.Errors;

        var classResult = IncomeClassPolicy.Validate(mainClass, subClass);
        if (classResult.IsError) return classResult.Errors;

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        var income = new Income(0, emailResult.Value, mainClass, subClass, content,
            moneyResult.Value, depositMyAssetProductName, note);

        if (created.HasValue)
            income.Created = created.Value;

        return income;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Updates all editable fields of this income record.</summary>
    public ErrorOr<Success> Update(
        string? mainClass,
        string? subClass,
        string? content,
        decimal amount,
        string? currency,
        string? depositMyAssetProductName,
        string? note,
        DateTime? created = null)
    {
        if (string.IsNullOrWhiteSpace(mainClass))
            return Error.Validation("Income.MainClassEmpty", "Main income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(subClass))
            return Error.Validation("Income.SubClassEmpty", "Sub income category cannot be empty.");

        if (string.IsNullOrWhiteSpace(depositMyAssetProductName))
            return Error.Validation("Income.DepositAssetEmpty", "Deposit asset product name cannot be empty.");

        var textResult = FinanceTextPolicy.ValidateContentAndNote(content, note);
        if (textResult.IsError) return textResult.Errors;

        var classResult = IncomeClassPolicy.Validate(mainClass, subClass);
        if (classResult.IsError) return classResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        MainClass = mainClass;
        SubClass = subClass;
        Content = content;
        Amount = moneyResult.Value;
        DepositMyAssetProductName = depositMyAssetProductName;
        Note = note;
        if (created.HasValue)
            Created = created.Value;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
