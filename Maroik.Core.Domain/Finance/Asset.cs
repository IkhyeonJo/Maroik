using ErrorOr;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Finance;

/// <summary>
/// Aggregate root representing a financial asset owned by an account
/// (e.g. a bank account, stock portfolio, cash wallet).
/// The composite identity is (<see cref="ProductName"/>, <see cref="AccountEmail"/>).
/// </summary>
public sealed class Asset : AggregateRoot<(string ProductName, string AccountEmail)>
{
    /// <summary>Product/account name that identifies the asset within the owner's portfolio.</summary>
    public string ProductName { get; private set; }

    /// <summary>Email of the owning account.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Asset category (e.g. "Deposit", "Stock", "Cash").</summary>
    public string Item { get; private set; }

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

    private Asset(
        string productName,
        Email accountEmail,
        string item,
        Money balance) : base((productName, accountEmail.Value))
    {
        ProductName = productName;
        AccountEmail = accountEmail;
        Item = item;
        Balance = balance;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
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
        var asset = new Asset(
            productName,
            Email.FromTrustedSource(accountEmail),
            item,
            Money.FromTrustedSource(amount, monetaryUnit))
        {
            Note = note, Deleted = deleted, Created = created, Updated = updated
        };

        return asset;
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
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Error.Validation("Asset.ProductNameEmpty", "Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(item))
            return Error.Validation("Asset.ItemEmpty", "Asset category (item) cannot be empty.");

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var moneyResult = Money.Create(amount, currency);
        if (moneyResult.IsError) return moneyResult.Errors;

        var asset = new Asset(productName, emailResult.Value, item, moneyResult.Value)
        {
            Note = note
        };

        return asset;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Adds the given amount to the balance (e.g. on income deposit).</summary>
    public ErrorOr<Success> Deposit(decimal amount, string currency)
    {
        Money money = Money.FromTrustedSource(amount, currency);
        if (money.Currency != Balance.Currency)
            return Error.Validation("Asset.CurrencyMismatch", "Cannot deposit a different currency into this asset.");

        Balance = Balance.Add(money);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Subtracts the given amount from the balance (e.g. on expenditure payment).</summary>
    public ErrorOr<Success> Withdraw(decimal amount, string currency)
    {
        Money money = Money.FromTrustedSource(amount, currency);
        if (money.Currency != Balance.Currency)
            return Error.Validation("Asset.CurrencyMismatch", "Cannot withdraw a different currency from this asset.");

        Balance = Balance.Subtract(money);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Directly sets the balance to the given value (e.g. reconciliation).</summary>
    public ErrorOr<Success> SetBalance(Money newBalance)
    {
        if (newBalance.Currency != Balance.Currency)
            return Error.Validation("Asset.CurrencyMismatch", "Cannot change the currency of an existing asset.");

        Balance = newBalance;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Updates all editable fields of the asset, including renaming and currency changes.
    /// Used by the service layer when an admin/user modifies asset details.
    /// </summary>
    public ErrorOr<Success> Update(string productName, string item, decimal amount, string currency, string? note, bool deleted)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Error.Validation("Asset.ProductNameEmpty", "Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(item))
            return Error.Validation("Asset.ItemEmpty", "Asset category cannot be empty.");

        ProductName = productName;
        Item = item;
        Balance = Money.FromTrustedSource(amount, currency);
        Note = note;
        Deleted = deleted;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Updates the optional memo attached to this asset.</summary>
    public void UpdateNote(string? note)
    {
        Note = note;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Marks the asset as logically deleted.</summary>
    public void SoftDelete()
    {
        Deleted = true;
        Updated = DateTime.UtcNow;
    }
}
