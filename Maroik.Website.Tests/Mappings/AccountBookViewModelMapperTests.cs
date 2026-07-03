using Maroik.Core.Contract.Dtos;
using Maroik.Website.Mappings;

namespace Maroik.Website.Tests.Mappings;

/// <summary>Unit tests for <see cref="AccountBookViewModelMapper"/>.</summary>
public class AccountBookViewModelMapperTests
{
    private static Func<string, string> Identity => key => key;
    private static Func<string, string> Prefix(string p) => key => $"{p}{key}";

    // ── Asset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AssetToDisplayViewModel_LocalizesItem()
    {
        var response = new AssetResponse { Item = "Deposit" };
        var vm = response.ToDisplayViewModel(Prefix("L:"), "UTC");
        Assert.Equal("L:Deposit", vm.Item);
    }

    [Fact]
    public void AssetToDisplayViewModel_CopiesScalarFields()
    {
        var response = new AssetResponse
        {
            ProductName = "Savings",
            Amount = 1234.5m,
            MonetaryUnit = "KRW",
            Note = "test note",
            Deleted = true
        };
        var vm = response.ToDisplayViewModel(Identity, "UTC");
        Assert.Equal("Savings", vm.ProductName);
        Assert.Equal(1234.5m, vm.Amount);
        Assert.Equal("KRW", vm.MonetaryUnit);
        Assert.Equal("test note", vm.Note);
        Assert.True(vm.Deleted);
    }

    [Fact]
    public void AssetToDisplayViewModel_ConvertsTimestamp_ToSeoul()
    {
        // 2025-01-01 00:00:00 UTC → 2025-01-01 09:00:00 KST (UTC+9)
        var utc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var vm = new AssetResponse { Created = utc, Updated = utc }
            .ToDisplayViewModel(Identity, "Asia/Seoul");
        Assert.Equal(9, vm.Created.Hour);
        Assert.Equal(9, vm.Updated.Hour);
    }

    [Fact]
    public void AssetsToDisplayViewModels_MapsAll()
    {
        var responses = new[]
        {
            new AssetResponse { ProductName = "A" },
            new AssetResponse { ProductName = "B" }
        };
        var vms = responses.ToDisplayViewModels(Identity, "UTC");
        Assert.Equal(2, vms.Count);
    }

    // ── Income ────────────────────────────────────────────────────────────────

    [Fact]
    public void IncomeToDisplayViewModel_LocalizesClassFields()
    {
        var response = new IncomeResponse { MainClass = "RegularIncome", SubClass = "LaborIncome" };
        var vm = response.ToDisplayViewModel(
            new Dictionary<string, string?> { ["prod"] = "KRW" },
            Prefix("L:"),
            "UTC");
        Assert.Equal("L:RegularIncome", vm.MainClass);
        Assert.Equal("L:LaborIncome", vm.SubClass);
    }

    [Fact]
    public void IncomeToDisplayViewModel_PopulatesMonetaryUnitFromAsset()
    {
        var response = new IncomeResponse { DepositMyAssetProductName = "MyBank" };
        var currency = new Dictionary<string, string?> { ["MyBank"] = "USD" };
        var vm = response.ToDisplayViewModel(currency, Identity, "UTC");
        Assert.Equal("USD", vm.MonetaryUnit);
    }

    [Fact]
    public void IncomeToDisplayViewModel_MonetaryUnit_NullWhenAssetNotFound()
    {
        var response = new IncomeResponse { DepositMyAssetProductName = "Unknown" };
        var vm = response.ToDisplayViewModel(new Dictionary<string, string?>(), Identity, "UTC");
        Assert.Null(vm.MonetaryUnit);
    }

    // ── Expenditure ───────────────────────────────────────────────────────────

    [Fact]
    public void ExpenditureToDisplayViewModel_LocalizesClassFields()
    {
        var response = new ExpenditureResponse { MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses" };
        var vm = response.ToDisplayViewModel(
            new Dictionary<string, string?> { ["card"] = "KRW" },
            Prefix("L:"),
            "UTC");
        Assert.Equal("L:ConsumerSpending", vm.MainClass);
        Assert.Equal("L:MealOrEatOutExpenses", vm.SubClass);
    }

    [Fact]
    public void ExpenditureToDisplayViewModel_PopulatesMonetaryUnitFromPaymentMethod()
    {
        var response = new ExpenditureResponse { PaymentMethod = "CreditCard" };
        var currency = new Dictionary<string, string?> { ["CreditCard"] = "KRW" };
        var vm = response.ToDisplayViewModel(currency, Identity, "UTC");
        Assert.Equal("KRW", vm.MonetaryUnit);
    }
}
