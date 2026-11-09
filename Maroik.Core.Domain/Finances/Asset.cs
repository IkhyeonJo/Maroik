using ErrorOr;
using Maroik.Core.Domain.Errors;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Finances;

/// <summary>
/// Aggregate root representing a financial asset owned by an account
/// (e.g. a bank account, stock portfolio, cash wallet).
/// The composite identity is (<see cref="ProductName"/>, <see cref="AccountEmail"/>).
/// <para>
/// <see cref="Update"/> can change <see cref="ProductName"/> (a rename, persisted via
/// <c>AssetRepository.UpdateAssetWithProductNameAsync</c> keyed on the original name, with
/// <c>ON UPDATE CASCADE</c> propagating it to referencing rows), and <see cref="Entity{TId}.Id"/> moves with
/// it. An <see cref="Asset"/> held in a hash-keyed collection across a rename therefore has to be added again.
/// </para>
/// </summary>
public sealed class Asset : AggregateRoot<(string ProductName, string AccountEmail)>
{
    /// <summary>Product/account name that identifies the asset within the owner's portfolio.</summary>
    public string ProductName { get; private set; }

    /// <summary>Email of the owning account.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Asset category — an <see cref="AssetItemType"/> member name (e.g. "FreeDepositAndWithdrawal", "CashAsset").</summary>
    public AssetItem Item { get; private set; }

    /// <summary>Current balance of the asset.</summary>
    public Money Balance { get; private set; }

    /// <summary>UTC timestamp when the asset was registered.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Optional memo about the asset.</summary>
    public string? Note { get; private set; }

    /// <summary>Soft-delete flag.</summary>
    public bool Deleted { get; private set; }

    /// <summary>"Newly registered asset" constructor: stamps <see cref="Created"/>/<see cref="Updated"/>. Used by <see cref="Create"/> only.</summary>
    private Asset(
        string productName,
        Email accountEmail,
        AssetItem item,
        Money balance,
        DateTime utcNow) : base((productName, accountEmail.Value))
    {
        ProductName = productName;
        AccountEmail = accountEmail;
        Item = item;
        Balance = balance;
        Created = utcNow;
        Updated = utcNow;
    }

    /// <summary>Reconstitution constructor: assigns every field verbatim from trusted storage with no new timestamps.</summary>
    private Asset(
        string productName, Email accountEmail, AssetItem item, Money balance,
        string? note, bool deleted, DateTime created, DateTime updated) : base((productName, accountEmail.Value))
    {
        ProductName = productName;
        AccountEmail = accountEmail;
        Item = item;
        Balance = balance;
        Note = note;
        Deleted = deleted;
        Created = created;
        Updated = updated;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds an <see cref="Asset"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static Asset Reconstitute(
        string productName,
        string accountEmail,
        string item,
        decimal amount,
        string monetaryUnit,
        string? note,
        bool deleted,
        DateTime created,
        DateTime updated)
    {
        return new Asset(
            productName,
            Email.FromTrustedSource(accountEmail),
            AssetItem.FromTrustedSource(item),
            Money.FromTrustedSource(amount, monetaryUnit),
            note,
            deleted,
            created,
            updated);
    }

    /// <summary>
    /// Registers a new asset.
    /// </summary>
    public static ErrorOr<Asset> Create(
        string? productName,
        string? accountEmailValue,
        string? item,
        decimal amount,
        string? currency,
        DateTime utcNow,
        string? note = null)
    {
        var fieldsResult = ValidateEditableFields(productName, item, amount, currency, note);
        if (fieldsResult.IsError) return fieldsResult.Errors;

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var (validName, validItem, balance) = fieldsResult.Value;
        var asset = new Asset(validName, emailResult.Value, validItem, balance, utcNow)
        {
            Note = note
        };

        return asset;
    }

    // ------------------------------------------------------------------------
    // Domain behaviors
    // ------------------------------------------------------------------------

    /// <summary>
    /// The one validation of what a creation and an edit both set. Product name and note are persisted in
    /// <c>character varying(255)</c> columns: an over-long value is rejected here (a clean validation error) instead of
    /// letting the write fail with a raw "value too long" (SQLSTATE 22001).
    /// </summary>
    private static ErrorOr<(string ProductName, AssetItem Item, Money Balance)> ValidateEditableFields(
        string? productName, string? item, decimal amount, string? currency, string? note)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return DomainError.Validation("Asset.ProductNameEmpty", "Product name cannot be empty.");

        if (productName.Length > FinanceTextPolicy.MaxTextLength)
            return DomainError.Validation("Asset.ProductNameTooLong", "Product name must be {0} characters or fewer.", FinanceTextPolicy.MaxTextLength);

        if (note is { Length: > FinanceTextPolicy.MaxTextLength })
            return DomainError.Validation("Finance.NoteTooLong", "Note must be {0} characters or fewer.", FinanceTextPolicy.MaxTextLength);

        var rangeResult = FinanceAmountPolicy.ValidateBalance(amount);
        if (rangeResult.IsError) return rangeResult.Errors;

        var itemResult = AssetItem.Create(item);
        if (itemResult.IsError) return itemResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        return (productName, itemResult.Value, moneyResult.Value);
    }

    /// <summary>Adds the given amount to the balance (e.g. on income deposit).</summary>
    public ErrorOr<Success> Deposit(Money amount, DateTime utcNow)
    {
        if (Deleted)
            return DomainError.Conflict("Asset.Deleted", "A deleted asset's balance cannot change.");

        if (amount.Currency != Balance.Currency)
            return DomainError.Validation("Asset.CurrencyMismatch", "Cannot deposit a different currency into this asset.");

        var addResult = Balance.Add(amount);
        if (addResult.IsError) return addResult.Errors;

        // The balance is persisted in a numeric(20,4) column: a deposit that would take it past the
        // range is a clean validation error here, not a raw numeric-overflow (SQLSTATE 22003) at save.
        var rangeResult = FinanceAmountPolicy.ValidateWithinRange(addResult.Value.Amount);
        if (rangeResult.IsError) return rangeResult.Errors;

        Balance = addResult.Value;
        Updated = utcNow;
        return Result.Success;
    }

    /// <summary>Carries out <paramref name="movement"/>: a <see cref="Deposit"/> or a <see cref="Withdraw"/> of its amount.</summary>
    public ErrorOr<Success> Apply(BalanceMovement movement, DateTime utcNow) =>
        movement.IsDeposit ? Deposit(movement.Amount, utcNow) : Withdraw(movement.Amount, utcNow);

    /// <summary>Subtracts the given amount from the balance (e.g. on expenditure payment).</summary>
    public ErrorOr<Success> Withdraw(Money amount, DateTime utcNow)
    {
        if (Deleted)
            return DomainError.Conflict("Asset.Deleted", "A deleted asset's balance cannot change.");

        if (amount.Currency != Balance.Currency)
            return DomainError.Validation("Asset.CurrencyMismatch", "Cannot withdraw a different currency from this asset.");

        var subtractResult = Balance.Subtract(amount);
        if (subtractResult.IsError) return subtractResult.Errors;

        // Same numeric(20,4) bound as Deposit, in the negative direction.
        var rangeResult = FinanceAmountPolicy.ValidateWithinRange(subtractResult.Value.Amount);
        if (rangeResult.IsError) return rangeResult.Errors;

        Balance = subtractResult.Value;
        Updated = utcNow;
        return Result.Success;
    }

    /// <summary>
    /// Corrects the asset's details by hand: its name (a rename moves <see cref="Entity{TId}.Id"/>), category, balance,
    /// currency and note. Whether it is deleted is not one of them: that is <see cref="SoftDelete"/> and <see cref="Restore"/>.
    /// </summary>
    public ErrorOr<Success> Update(string productName, string item, decimal amount, string currency, string? note, DateTime utcNow)
    {
        var fieldsResult = ValidateEditableFields(productName, item, amount, currency, note);
        if (fieldsResult.IsError) return fieldsResult.Errors;

        (ProductName, Item, Balance) = fieldsResult.Value;
        Id = (ProductName, AccountEmail.Value);
        Note = note;
        Updated = utcNow;
        return Result.Success;
    }

    /// <summary>Marks the asset as logically deleted.</summary>
    public void SoftDelete(DateTime utcNow)
    {
        Deleted = true;
        Updated = utcNow;
    }

    /// <summary>Brings a logically deleted asset back.</summary>
    public void Restore(DateTime utcNow)
    {
        Deleted = false;
        Updated = utcNow;
    }
}
