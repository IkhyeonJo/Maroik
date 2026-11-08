using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.ValueObjects;
namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Finances.Asset"/>.
/// Covers creation validation, deposit/withdraw operations, balance setting with currency-mismatch guard,
/// update logic, note management, and soft-delete.
/// </summary>
public class AssetTests
{
    /// <summary>The fixed "current time" every domain call in this class receives.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates a valid asset; each argument can be overridden per test.</summary>
    private static Asset ValidAsset(
        string productName = "My Bank",
        string email = "user@example.com",
        string item = "FreeDepositAndWithdrawal",
        decimal amount = 1000m,
        string currency = "KRW")
        => Asset.Create(productName, email, item, amount, currency, _now).Value;

    // -- Create ---------------------------------------------------------------

    /// <summary>A new asset's Created and Updated are the instant passed in; later changes stamp only Updated.</summary>
    [Fact]
    public void Create_StampsCreatedAndUpdated_WithTheGivenInstant_AndDepositStampsUpdated()
    {
        var asset = ValidAsset();

        Assert.Equal(_now, asset.Created);
        Assert.Equal(asset.Created, asset.Updated);

        Assert.False(asset.Deposit(Money.Create(1m, "KRW").Value, _now.AddMinutes(5)).IsError);
        Assert.Equal(_now, asset.Created);
        Assert.Equal(_now.AddMinutes(5), asset.Updated);
    }

    /// <summary>Create returns asset, when valid.</summary>
    [Fact]
    public void Create_ReturnsAsset_WhenValid()
    {
        var result = Asset.Create("Savings", "user@example.com", "FreeDepositAndWithdrawal", 5000m, "KRW", _now);

        Assert.False(result.IsError);
        Assert.Equal("Savings", result.Value.ProductName);
        Assert.Equal(5000m, result.Value.Balance.Amount);
    }

    /// <summary>Create returns error, when product name empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenProductNameEmpty(string? name)
    {
        var result = Asset.Create(name, "user@example.com", "FreeDepositAndWithdrawal", 0m, "KRW", _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ProductNameEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when item empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenItemEmpty(string? item)
    {
        var result = Asset.Create("Savings", "user@example.com", item, 0m, "KRW", _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemEmpty", result.FirstError.Code);
    }

    /// <summary>
    /// Regression: Create rejects an <c>Item</c> value outside <see cref="AssetItemType"/>'s known
    /// members — previously only <c>AssetService</c> checked this, so the aggregate itself could be
    /// constructed (and would then fail with a raw DB <c>Asset_Item_check</c> exception on save) with
    /// an unrecognized category.
    /// </summary>
    [Theory]
    [InlineData("Deposit")]
    [InlineData("Stock")]
    [InlineData("freeDepositAndWithdrawal")]
    public void Create_ReturnsError_WhenItemNotRecognized(string item)
    {
        var result = Asset.Create("Savings", "user@example.com", item, 0m, "KRW", _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemInvalid", result.FirstError.Code);
    }

    /// <summary>Create returns error, when email invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenEmailInvalid()
    {
        var result = Asset.Create("Savings", "bademail", "FreeDepositAndWithdrawal", 0m, "KRW", _now);

        Assert.True(result.IsError);
    }

    /// <summary>Create returns error, when currency empty.</summary>
    [Fact]
    public void Create_ReturnsError_WhenCurrencyEmpty()
    {
        var result = Asset.Create("Savings", "user@example.com", "FreeDepositAndWithdrawal", 0m, null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Money.CurrencyEmpty", result.FirstError.Code);
    }

    // -- Deposit / Withdraw ----------------------------------------------------

    /// <summary>Deposit increases balance.</summary>
    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var asset = ValidAsset(amount: 1000m);

        asset.Deposit(Money.Create(500m, "KRW").Value, _now);

        Assert.Equal(1500m, asset.Balance.Amount);
    }

    /// <summary>Withdraw decreases balance.</summary>
    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var asset = ValidAsset(amount: 1000m);

        asset.Withdraw(Money.Create(300m, "KRW").Value, _now);

        Assert.Equal(700m, asset.Balance.Amount);
    }

    /// <summary>
    /// Regression: the balance lives in a numeric(20,4) column, so a deposit that would take it past
    /// <see cref="FinanceAmountPolicy.MaxAbsoluteAmount"/> is a clean validation error (and leaves the
    /// balance untouched) instead of a raw numeric-overflow at save.
    /// </summary>
    [Fact]
    public void Deposit_ReturnsError_AndLeavesBalanceUntouched_WhenItWouldExceedTheRange()
    {
        var asset = ValidAsset(amount: FinanceAmountPolicy.MaxAbsoluteAmount);

        var result = asset.Deposit(Money.Create(0.01m, "KRW").Value, _now);

        Assert.True(result.IsError);
        Assert.Equal("Finance.AmountOutOfRange", result.FirstError.Code);
        Assert.Equal(FinanceAmountPolicy.MaxAbsoluteAmount, asset.Balance.Amount);
    }

    /// <summary>The same bound applies in the negative direction on a withdrawal.</summary>
    [Fact]
    public void Withdraw_ReturnsError_AndLeavesBalanceUntouched_WhenItWouldExceedTheNegativeRange()
    {
        var asset = ValidAsset(amount: -FinanceAmountPolicy.MaxAbsoluteAmount);

        var result = asset.Withdraw(Money.Create(0.01m, "KRW").Value, _now);

        Assert.True(result.IsError);
        Assert.Equal("Finance.AmountOutOfRange", result.FirstError.Code);
        Assert.Equal(-FinanceAmountPolicy.MaxAbsoluteAmount, asset.Balance.Amount);
    }

    /// <summary>A change that lands exactly on the limit is still allowed.</summary>
    [Fact]
    public void Deposit_Succeeds_WhenItLandsExactlyOnTheLimit()
    {
        var asset = ValidAsset(amount: FinanceAmountPolicy.MaxAbsoluteAmount - 1m);

        var result = asset.Deposit(Money.Create(1m, "KRW").Value, _now);

        Assert.False(result.IsError);
        Assert.Equal(FinanceAmountPolicy.MaxAbsoluteAmount, asset.Balance.Amount);
    }

    /// <summary>Deposit returns error, when currency mismatch.</summary>
    [Fact]
    public void Deposit_ReturnsError_WhenCurrencyMismatch()
    {
        var asset = ValidAsset(amount: 1000m, currency: "KRW");

        var result = asset.Deposit(Money.Create(500m, "USD").Value, _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.CurrencyMismatch", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }

    /// <summary>Withdraw returns error, when currency mismatch.</summary>
    [Fact]
    public void Withdraw_ReturnsError_WhenCurrencyMismatch()
    {
        var asset = ValidAsset(amount: 1000m, currency: "KRW");

        var result = asset.Withdraw(Money.Create(300m, "USD").Value, _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.CurrencyMismatch", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }

    // -- Update ----------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var asset = ValidAsset();

        var result = asset.Update("New Name", "InvestmentAsset", 2000m, "KRW", "memo", _now);

        Assert.False(result.IsError);
        Assert.Equal("New Name", asset.ProductName);
        Assert.Equal("InvestmentAsset", asset.Item.Value);
        Assert.Equal(2000m, asset.Balance.Amount);
    }

    /// <summary>A rename moves the asset's identity with its name, so the instance equals a fresh load of the renamed row.</summary>
    [Fact]
    public void Update_MovesTheIdentity_WhenTheProductNameChanges()
    {
        var asset = ValidAsset(productName: "Old Name");
        Asset reloaded = Asset.Reconstitute("New Name", asset.AccountEmail.Value, "CashAsset", 1m, "KRW", null, false, _now, _now);

        Assert.False(asset.Update("New Name", "CashAsset", 1m, "KRW", null, _now).IsError);

        Assert.Equal(("New Name", asset.AccountEmail.Value), asset.Id);
        Assert.Equal(reloaded, asset);
    }

    /// <summary>An edit leaves the deleted flag to <see cref="Asset.SoftDelete"/> and <see cref="Asset.Restore"/>.</summary>
    [Fact]
    public void Update_LeavesTheDeletedFlagAlone()
    {
        var asset = ValidAsset();
        asset.SoftDelete(_now);

        Assert.False(asset.Update("My Bank", "CashAsset", 1m, "KRW", null, _now.AddMinutes(1)).IsError);

        Assert.True(asset.Deleted);
    }

    /// <summary>Restore brings a deleted asset back and stamps Updated.</summary>
    [Fact]
    public void Restore_ClearsTheDeletedFlag_AndStampsUpdated()
    {
        var asset = ValidAsset();
        asset.SoftDelete(_now);

        asset.Restore(_now.AddMinutes(5));

        Assert.False(asset.Deleted);
        Assert.Equal(_now.AddMinutes(5), asset.Updated);
    }

    /// <summary>A refused edit changes nothing, the identity included.</summary>
    [Fact]
    public void Update_ChangesNothing_WhenRefused()
    {
        var asset = ValidAsset(productName: "Old Name");
        var id = asset.Id;

        Assert.True(asset.Update("New Name", "Nonsense", 1m, "KRW", "memo", _now.AddMinutes(1)).IsError);

        Assert.Equal("Old Name", asset.ProductName);
        Assert.Equal(id, asset.Id);
        Assert.Equal(1000m, asset.Balance.Amount);
        Assert.Null(asset.Note);
        Assert.Equal(_now, asset.Updated);
    }

    /// <summary>Update returns error, when product name empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenProductNameEmpty()
    {
        var asset = ValidAsset();

        var result = asset.Update("", "FreeDepositAndWithdrawal", 0m, "KRW", null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ProductNameEmpty", result.FirstError.Code);
    }

    // -- Update (item validation) ----------------------------------------------

    /// <summary>Update returns error, when item empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenItemEmpty()
    {
        var asset = ValidAsset();

        var result = asset.Update("Name", "", 0m, "KRW", null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemEmpty", result.FirstError.Code);
    }

    /// <summary>Regression: Update rejects an <c>Item</c> value outside <see cref="AssetItemType"/>'s known members (see <see cref="Create_ReturnsError_WhenItemNotRecognized"/>).</summary>
    [Fact]
    public void Update_ReturnsError_WhenItemNotRecognized()
    {
        var asset = ValidAsset();

        var result = asset.Update("Name", "Stock", 1000m, "KRW", null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemInvalid", result.FirstError.Code);
    }

    /// <summary>Update returns error and leaves the balance unchanged, when currency is empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenCurrencyEmpty()
    {
        var asset = ValidAsset();
        string originalCurrency = asset.Balance.Currency.Value;

        var result = asset.Update("Name", "FreeDepositAndWithdrawal", 1000m, "", null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Money.CurrencyEmpty", result.FirstError.Code);
        Assert.Equal(originalCurrency, asset.Balance.Currency.Value);
    }

    // -- SoftDelete ------------------------------------------------------------

    /// <summary>Soft delete sets deleted true.</summary>
    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var asset = ValidAsset();

        asset.SoftDelete(_now);

        Assert.True(asset.Deleted);
    }
}
