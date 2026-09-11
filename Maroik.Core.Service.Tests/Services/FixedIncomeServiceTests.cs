using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FixedIncomeService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers fixed-income CRUD, maturity-date expiry detection,
/// notification-window (Noticed) flag computation, and unpunctuality tracking.
/// </summary>
public class FixedIncomeServiceTests
{
    private readonly Mock<IFixedIncomeRepository> _fixedIncomeRepo = new();
    private readonly Mock<IAssetBalanceDomainService> _assetBalance = new();

    /// <summary>Bridges the id-lookup and no-lock asset read the service now uses to the list-/lock-based
    /// setups these tests already declare.</summary>
    public FixedIncomeServiceTests()
    {
        _fixedIncomeRepo.Setup(r => r.FindByEmailAndIdAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(async (string e, long id, CancellationToken c) =>
                (await _fixedIncomeRepo.Object.GetByAccountEmailAsync(e, c)).FirstOrDefault(x => x.Id == id));
        _assetBalance.Setup(a => a.GetAssetForReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string e, string p, CancellationToken c) => _assetBalance.Object.GetAssetAsync(e, p, c));
    }

    private FixedIncomeService CreateSut() => new(_fixedIncomeRepo.Object, _assetBalance.Object);

    // -- Helpers --------------------------------------------------------------

    private static Asset MakeSavingsAsset(string name = "SavingsAccount", string currency = "KRW") =>
        Asset.Reconstitute(name, "user@example.com", "SavingsAsset", 0m, currency, null, false, DateTime.UtcNow, DateTime.UtcNow);

    private static FixedIncomeRequest ValidRequest(string mainClass = "RegularIncome", string subClass = "LaborIncome") => new()
    {
        MainClass = mainClass,
        SubClass = subClass,
        DepositMyAssetProductName = "SavingsAccount",
        Amount = 500m,
        DepositMonth = 1,
        DepositDay = 15,
        MaturityDate = new DateTime(2030, 12, 31)
    };

    // -- CreateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when class is invalid.</summary>
    [Theory]
    [InlineData("InvalidClass", "LaborIncome")]
    [InlineData("RegularIncome", "InvalidSub")]
    [InlineData("IrregularIncome", "FinancialIncome")] // FinancialIncome not valid for Irregular
    [InlineData(null, "LaborIncome")]
    public async Task CreateAsync_ReturnsFail_WhenClassIsInvalid(string? mainClass, string subClass)
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.MainClass = mainClass;
        request.SubClass = subClass;

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit month is out of range.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task CreateAsync_ReturnsFail_WhenDepositMonthIsOutOfRange(short month)
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.DepositMonth = month;

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit day is out of range.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task CreateAsync_ReturnsFail_WhenDepositDayIsOutOfRange(short day)
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.DepositDay = day;

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>
    /// The out-of-range message is a composite-format template with the computed max day and the
    /// requested month as separate <see cref="ServiceResult.ErrorArgs"/> -- not baked into
    /// <see cref="ServiceResult.ErrorKey"/> -- so the UI can localize it via resx.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ReturnsLocalizableErrorArgs_WhenDepositDayIsOutOfRange()
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.DepositMonth = 1;
        request.DepositDay = 32;

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.Equal("Deposit day must be between 1 and {0} for month {1}.", result.ErrorKey);
        Assert.Equal([31, (short)1], result.ErrorArgs);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for all valid class combinations.</summary>
    [Theory]
    [InlineData("RegularIncome", "LaborIncome")]
    [InlineData("RegularIncome", "BusinessIncome")]
    [InlineData("RegularIncome", "PensionIncome")]
    [InlineData("RegularIncome", "FinancialIncome")]
    [InlineData("RegularIncome", "RentalIncome")]
    [InlineData("RegularIncome", "OtherIncome")]
    [InlineData("IrregularIncome", "LaborIncome")]
    [InlineData("IrregularIncome", "OtherIncome")]
    public async Task CreateAsync_ReturnsSuccess_ForAllValidClassCombinations(string mainClass, string subClass)
    {
        _assetBalance.Setup(r => r.GetAssetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSavingsAsset());
        _fixedIncomeRepo.Setup(r => r.CreateAsync(It.IsAny<FixedIncome>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest(mainClass, subClass), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> sets account email and absolute amount.</summary>
    [Fact]
    public async Task CreateAsync_SetsAccountEmailAndAbsoluteAmount()
    {
        _assetBalance.Setup(r => r.GetAssetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSavingsAsset());
        FixedIncome? captured = null;
        _fixedIncomeRepo.Setup(r => r.CreateAsync(It.IsAny<FixedIncome>(), It.IsAny<CancellationToken>()))
            .Callback<FixedIncome, CancellationToken>((fi, _) => captured = fi)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ValidRequest();
        request.Amount = -250m;
        await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured.AccountEmail.Value);
        Assert.Equal(250m, captured.Amount.Amount);
        Assert.True(captured.Created > DateTime.MinValue);
    }

    // -- UpdateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when class is invalid.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassIsInvalid()
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.MainClass = "BadClass";
        request.Id = 1;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedIncome>());
        var sut = CreateSut();

        var request = ValidRequest();
        request.Id = 99;
        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns success when valid and record found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenValidAndRecordFound()
    {
        const string email = "user@example.com";
        var existing = FixedIncome.Reconstitute(1, email, "RegularIncome", "LaborIncome", null, 0m, "KRW",
            "SavingsAccount", (short)1, (short)15, new DateTime(2025, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow);
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedIncome> { existing });
        _assetBalance.Setup(r => r.GetAssetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSavingsAsset());
        _fixedIncomeRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<FixedIncome>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ValidRequest();
        request.Id = 1;
        request.Amount = 600m;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _fixedIncomeRepo.Verify(r => r.UpdateEntityAsync(It.Is<FixedIncome>(fi => fi.Amount.Amount == 600m), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- DeleteAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedIncome>());
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> returns success when record found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenRecordFound()
    {
        const string email = "user@example.com";
        var existing = FixedIncome.Reconstitute(1, email, "RegularIncome", "LaborIncome", null, 0m, "KRW",
            "SavingsAccount", (short)1, (short)15, new DateTime(2030, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow);
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<FixedIncome> { existing });
        _fixedIncomeRepo.Setup(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _fixedIncomeRepo.Verify(r => r.DeleteByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- GetFixedIncomesAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetFixedIncomesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetFixedIncomesAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<FixedIncome>
        {
            FixedIncome.Reconstitute(1, email, "RegularIncome", "LaborIncome", null, 500m, "KRW",
                "SavingsAccount", (short)1, (short)15, new DateTime(2030, 12, 31), null, false, DateTime.UtcNow, DateTime.UtcNow)
        };
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var sut = CreateSut();

        List<FixedIncomeResponse> result = await sut.GetFixedIncomesAsync(email, TestContext.Current.CancellationToken);

        Assert.Single(result);
    }
}
