using Maroik.Core.Domain.Finance;
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
    private readonly Mock<IAssetBalanceDomainService> _assetBalance = new();

    /// <summary>Bridges the id-lookup and no-lock asset read the service now uses to the list-/lock-based
    /// setups these tests already declare.</summary>
    public FixedExpenditureServiceTests()
    {
        _fixedExpenditureRepo.Setup(r => r.FindByEmailAndIdAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(async (string e, long id, CancellationToken c) =>
                (await _fixedExpenditureRepo.Object.GetByAccountEmailAsync(e, c)).FirstOrDefault(x => x.Id == id));
        _assetBalance.Setup(a => a.GetAssetForReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string e, string p, CancellationToken c) => _assetBalance.Object.GetAssetAsync(e, p, c));
    }

    private FixedExpenditureService CreateSut() => new(
        _fixedExpenditureRepo.Object,
        _assetBalance.Object);

    // -- Helpers --------------------------------------------------------------

    private static Asset MakeAsset(string name, string unit = "KRW") =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", 1000m, unit, null, false, DateTime.UtcNow, DateTime.UtcNow);

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

    // -- CreateAsync ----------------------------------------------------------

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment method asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentMethodAssetNotFound()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when same payment and deposit asset.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSamePaymentAndDepositAsset()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Wallet"));
        var sut = CreateSut();
        var request = SavingsRequest();
        request.PaymentMethod = "Wallet";
        request.MyDepositAsset = "Wallet";

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("same", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Checking"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings", It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when currencies mismatch.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenCurrenciesMismatch()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Checking", "KRW"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Savings", "USD"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("MonetaryUnit", result.ErrorKey);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for consumer spending with valid asset.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsSuccess_ForConsumerSpendingWithValidAsset()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.CreateAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.CreateAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for regular savings with matching currencies.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsSuccess_ForRegularSavingsWithMatchingCurrencies()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Checking", "KRW"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Savings", "KRW"));
        _fixedExpenditureRepo.Setup(r => r.CreateAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> sets amount to absolute value.</summary>
    [Fact]
    public async Task CreateAsync_SetsAmountToAbsoluteValue()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Wallet"));

        FixedExpenditure? captured = null;
        _fixedExpenditureRepo.Setup(r => r.CreateAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>()))
            .Callback<FixedExpenditure, CancellationToken>((fe, _) => captured = fe)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Amount = -150m;
        await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(150m, captured.Amount.Amount);
    }

    // -- UpdateAsync ----------------------------------------------------------

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

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when existing record not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenExistingRecordNotFound()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedExpenditure>());
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Id = 99;
        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns success when valid and record found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenValidAndRecordFound()
    {
        const string email = "user@example.com";
        var existing = FixedExpenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 100m, "KRW",
            "Wallet", null, (short)1, (short)15, new DateTime(2030, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow);
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Wallet", It.IsAny<CancellationToken>())).ReturnsAsync(MakeAsset("Wallet"));
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedExpenditure> { existing });
        _fixedExpenditureRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<FixedExpenditure>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- DeleteAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedExpenditure>());
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> returns success when record found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenRecordFound()
    {
        const string email = "user@example.com";
        var existing = FixedExpenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 100m, "KRW",
            "Wallet", null, (short)1, (short)15, new DateTime(2030, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow);
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedExpenditure> { existing });
        _fixedExpenditureRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- GetFixedExpendituresAsync --------------------------------------------

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<FixedExpenditure>
        {
            FixedExpenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 100m, "KRW",
                "Wallet", null, (short)1, (short)15, new DateTime(2030, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow)
        };
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var sut = CreateSut();

        List<FixedExpenditureResponse> result = await sut.GetFixedExpendituresAsync(email, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
