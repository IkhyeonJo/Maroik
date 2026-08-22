using Maroik.Core.Domain.Finance;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Finance.Asset"/>.
/// Covers creation validation, deposit/withdraw operations, balance setting with currency-mismatch guard,
/// update logic, note management, and soft-delete.
/// </summary>
public class AssetTests
{
    private static Asset ValidAsset(
        string productName = "My Bank",
        string email = "user@example.com",
        string item = "Deposit",
        decimal amount = 1000m,
        string currency = "KRW")
        => Asset.Create(productName, email, item, amount, currency).Value;

    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns asset, when valid.</summary>
    [Fact]
    public void Create_ReturnsAsset_WhenValid()
    {
        var result = Asset.Create("Savings", "user@example.com", "Deposit", 5000m, "KRW");

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
        var result = Asset.Create(name, "user@example.com", "Deposit", 0m, "KRW");

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
        var result = Asset.Create("Savings", "user@example.com", item, 0m, "KRW");

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when email invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenEmailInvalid()
    {
        var result = Asset.Create("Savings", "bademail", "Deposit", 0m, "KRW");

        Assert.True(result.IsError);
    }

    /// <summary>Create returns error, when currency empty.</summary>
    [Fact]
    public void Create_ReturnsError_WhenCurrencyEmpty()
    {
        var result = Asset.Create("Savings", "user@example.com", "Deposit", 0m, null);

        Assert.True(result.IsError);
        Assert.Equal("Money.CurrencyEmpty", result.FirstError.Code);
    }

    // -- Deposit / Withdraw ----------------------------------------------------

    /// <summary>Deposit increases balance.</summary>
    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var asset = ValidAsset(amount: 1000m);

        asset.Deposit(500m, "KRW");

        Assert.Equal(1500m, asset.Balance.Amount);
    }

    /// <summary>Withdraw decreases balance.</summary>
    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var asset = ValidAsset(amount: 1000m);

        asset.Withdraw(300m, "KRW");

        Assert.Equal(700m, asset.Balance.Amount);
    }

    /// <summary>Deposit returns error, when currency mismatch.</summary>
    [Fact]
    public void Deposit_ReturnsError_WhenCurrencyMismatch()
    {
        var asset = ValidAsset(amount: 1000m, currency: "KRW");

        var result = asset.Deposit(500m, "USD");

        Assert.True(result.IsError);
        Assert.Equal("Asset.CurrencyMismatch", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }

    /// <summary>Withdraw returns error, when currency mismatch.</summary>
    [Fact]
    public void Withdraw_ReturnsError_WhenCurrencyMismatch()
    {
        var asset = ValidAsset(amount: 1000m, currency: "KRW");

        var result = asset.Withdraw(300m, "USD");

        Assert.True(result.IsError);
        Assert.Equal("Asset.CurrencyMismatch", result.FirstError.Code);
        Assert.Equal(1000m, asset.Balance.Amount);
    }

    // -- SetBalance ------------------------------------------------------------

    /// <summary>Set balance succeeds, when same currency.</summary>
    [Fact]
    public void SetBalance_Succeeds_WhenSameCurrency()
    {
        var asset = ValidAsset(amount: 1000m, currency: "KRW");
        var newBalance = Money.Create(9999m, "KRW").Value;

        var result = asset.SetBalance(newBalance);

        Assert.False(result.IsError);
        Assert.Equal(9999m, asset.Balance.Amount);
    }

    /// <summary>Set balance returns error, when currency mismatch.</summary>
    [Fact]
    public void SetBalance_ReturnsError_WhenCurrencyMismatch()
    {
        var asset = ValidAsset(currency: "KRW");
        var usdBalance = Money.Create(100m, "USD").Value;

        var result = asset.SetBalance(usdBalance);

        Assert.True(result.IsError);
        Assert.Equal("Asset.CurrencyMismatch", result.FirstError.Code);
    }

    // -- Update ----------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var asset = ValidAsset();

        var result = asset.Update("New Name", "Stock", 2000m, "KRW", "memo", false);

        Assert.False(result.IsError);
        Assert.Equal("New Name", asset.ProductName);
        Assert.Equal("Stock", asset.Item);
        Assert.Equal(2000m, asset.Balance.Amount);
    }

    /// <summary>Update returns error, when product name empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenProductNameEmpty()
    {
        var asset = ValidAsset();

        var result = asset.Update("", "Deposit", 0m, "KRW", null, false);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ProductNameEmpty", result.FirstError.Code);
    }

    // -- Update (item validation) ----------------------------------------------

    /// <summary>Update returns error, when item empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenItemEmpty()
    {
        var asset = ValidAsset();

        var result = asset.Update("Name", "", 0m, "KRW", null, false);

        Assert.True(result.IsError);
        Assert.Equal("Asset.ItemEmpty", result.FirstError.Code);
    }

    // -- UpdateNote ------------------------------------------------------------

    /// <summary>Update note sets note.</summary>
    [Fact]
    public void UpdateNote_SetsNote()
    {
        var asset = ValidAsset();

        asset.UpdateNote("memo text");

        Assert.Equal("memo text", asset.Note);
    }

    /// <summary>Update note clears note, when null.</summary>
    [Fact]
    public void UpdateNote_ClearsNote_WhenNull()
    {
        var asset = ValidAsset();
        asset.UpdateNote("memo");

        asset.UpdateNote(null);

        Assert.Null(asset.Note);
    }

    // -- SoftDelete ------------------------------------------------------------

    /// <summary>Soft delete sets deleted true.</summary>
    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var asset = ValidAsset();

        asset.SoftDelete();

        Assert.True(asset.Deleted);
    }
}
