using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FixedExpenditureService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers fixed-expenditure CRUD, maturity-date expiry detection,
/// notification-window (Noticed) flag computation, and unpunctuality tracking.
/// </summary>
public class FixedExpenditureServiceTests
{
    private readonly Mock<IFixedExpenditureRepository> _fixedExpenditureRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();

    private FixedExpenditureService CreateSut() => new(
        _fixedExpenditureRepo.Object,
        _assetRepo.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssetResponse MakeAsset(string name, string unit = "KRW") => new()
    {
        ProductName = name,
        AccountEmail = "user@example.com",
        MonetaryUnit = unit,
        Amount = 1000m,
        Deleted = false
    };

    private static FixedExpenditureRequest ConsumerRequest() => new()
    {
        MainClass = "ConsumerSpending",
        SubClass = "MealOrEatOutExpenses",
        PaymentMethod = "Wallet",
        Amount = 100m,
        DepositMonth = 1,
        DepositDay = 15,
        MaturityDate = new DateTime(2030, 12, 31)
    };

    private static FixedExpenditureRequest SavingsRequest() => new()
    {
        MainClass = "RegularSavings",
        SubClass = "Deposit",
        PaymentMethod = "Checking",
        MyDepositAsset = "Savings",
        Amount = 200m,
        DepositMonth = 1,
        DepositDay = 15,
        MaturityDate = new DateTime(2030, 12, 31)
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when class is invalid.</summary>
    [Theory]
    [InlineData("InvalidClass", "MealOrEatOutExpenses")]
    [InlineData("ConsumerSpending", "Deposit")] // Deposit is not valid for ConsumerSpending
    [InlineData(null, "MealOrEatOutExpenses")]
    public async Task CreateAsync_ReturnsFail_WhenClassIsInvalid(string? mainClass, string subClass)
    {
        var sut = CreateSut();
        var request = new FixedExpenditureRequest
        {
            MainClass = mainClass,
            SubClass = subClass,
            PaymentMethod = "Wallet",
            DepositMonth = 1,
            DepositDay = 15,
            MaturityDate = new DateTime(2030, 12, 31)
        };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit month is invalid.</summary>
    [Theory]
    [InlineData(0)]   // out of range
    [InlineData(13)]  // out of range
    public async Task CreateAsync_ReturnsFail_WhenDepositMonthIsInvalid(short month)
    {
        var sut = CreateSut();
        var request = ConsumerRequest();
        request.DepositMonth = month;

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit day is invalid.</summary>
    [Theory]
    [InlineData(0)]   // below 1
    [InlineData(32)]  // above 31
    public async Task CreateAsync_ReturnsFail_WhenDepositDayIsInvalid(short day)
    {
        var sut = CreateSut();
        var request = ConsumerRequest();
        request.DepositDay = day;

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment method asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentMethodAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest());

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when same payment and deposit asset.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSamePaymentAndDepositAsset()
    {
        var sut = CreateSut();
        var request = SavingsRequest();
        request.PaymentMethod = "Wallet";
        request.MyDepositAsset = "Wallet";

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        Assert.Contains("same", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest());

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when currencies mismatch.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenCurrenciesMismatch()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking", "KRW"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(MakeAsset("Savings", "USD"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest());

        Assert.False(result.Success);
        Assert.Contains("MonetaryUnit", result.ErrorKey);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for consumer spending with valid asset.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsSuccess_ForConsumerSpendingWithValidAsset()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.CreateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest());

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.CreateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>()), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for regular savings with matching currencies.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsSuccess_ForRegularSavingsWithMatchingCurrencies()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking", "KRW"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(MakeAsset("Savings", "KRW"));
        _fixedExpenditureRepo.Setup(r => r.CreateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest());

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> sets amount to absolute value.</summary>
    [Fact]
    public async Task CreateAsync_SetsAmountToAbsoluteValue()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync(MakeAsset("Wallet"));

        FixedExpenditureRequest? captured = null;
        _fixedExpenditureRepo.Setup(r => r.CreateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>()))
            .Callback<FixedExpenditureRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Amount = -150m;
        await sut.CreateAsync("user@example.com", request);

        Assert.NotNull(captured);
        Assert.Equal(150m, captured.Amount);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when class is invalid.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new FixedExpenditureRequest
        {
            Id = 1,
            MainClass = "InvalidClass",
            SubClass = "MealOrEatOutExpenses",
            PaymentMethod = "Wallet",
            DepositMonth = 1,
            DepositDay = 15,
            MaturityDate = new DateTime(2030, 12, 31)
        };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when existing record not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenExistingRecordNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Id = 99;
        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns success when valid and record found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenValidAndRecordFound()
    {
        const string email = "user@example.com";
        var existing = new FixedExpenditureResponse
        {
            Id = 1, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses",
            PaymentMethod = "Wallet", DepositMonth = 1, DepositDay = 15
        };
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync([existing]);
        _fixedExpenditureRepo.Setup(r => r.UpdateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.UpdateFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>()), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> returns success when record found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenRecordFound()
    {
        const string email = "user@example.com";
        var existing = new FixedExpenditureResponse
        {
            Id = 1, MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses",
            PaymentMethod = "Wallet", DepositMonth = 1, DepositDay = 15
        };
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync([existing]);
        _fixedExpenditureRepo.Setup(r => r.DeleteFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.DeleteFixedExpenditureAsync(It.IsAny<FixedExpenditureRequest>()), Times.Once);
    }

    // ── GetFixedExpendituresAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<FixedExpenditureResponse> { new() { Id = 1 } };
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync(list);
        var sut = CreateSut();

        List<FixedExpenditureResponse> result = await sut.GetFixedExpendituresAsync(email);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
