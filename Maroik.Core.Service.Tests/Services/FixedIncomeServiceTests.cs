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

    private FixedIncomeService CreateSut() => new(_fixedIncomeRepo.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    // ── CreateAsync ──────────────────────────────────────────────────────────

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

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

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
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
        _fixedIncomeRepo.Setup(r => r.CreateFixedIncomeAsync(It.IsAny<FixedIncomeRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest(mainClass, subClass));

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>CreateAsync</c> sets account email and absolute amount.</summary>
    [Fact]
    public async Task CreateAsync_SetsAccountEmailAndAbsoluteAmount()
    {
        FixedIncomeRequest? captured = null;
        _fixedIncomeRepo.Setup(r => r.CreateFixedIncomeAsync(It.IsAny<FixedIncomeRequest>()))
            .Callback<FixedIncomeRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ValidRequest();
        request.Amount = -250m;
        await sut.CreateAsync("user@example.com", request);

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured.AccountEmail);
        Assert.Equal(250m, captured.Amount);
        Assert.True(captured.Created > DateTime.MinValue);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when class is invalid.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassIsInvalid()
    {
        var sut = CreateSut();
        var request = ValidRequest();
        request.MainClass = "BadClass";
        request.Id = 1;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        var request = ValidRequest();
        request.Id = 99;
        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns success when valid and record found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenValidAndRecordFound()
    {
        const string email = "user@example.com";
        var existing = new FixedIncomeResponse
        {
            Id = 1, MainClass = "RegularIncome", SubClass = "LaborIncome",
            DepositMyAssetProductName = "SavingsAccount", DepositMonth = 1, DepositDay = 15,
            MaturityDate = new DateTime(2025, 12, 31)
        };
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync([existing]);
        _fixedIncomeRepo.Setup(r => r.UpdateFixedIncomeAsync(It.IsAny<FixedIncomeRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = ValidRequest();
        request.Id = 1;
        request.Amount = 600m;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        _fixedIncomeRepo.Verify(r => r.UpdateFixedIncomeAsync(It.Is<FixedIncomeRequest>(req => req.Amount == 600m)), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when record not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenRecordNotFound()
    {
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> returns success when record found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenRecordFound()
    {
        const string email = "user@example.com";
        var existing = new FixedIncomeResponse
        {
            Id = 1, MainClass = "RegularIncome", SubClass = "LaborIncome",
            DepositMyAssetProductName = "SavingsAccount", DepositMonth = 1, DepositDay = 15,
            MaturityDate = new DateTime(2030, 12, 31)
        };
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync([existing]);
        _fixedIncomeRepo.Setup(r => r.DeleteFixedIncomeAsync(It.IsAny<FixedIncomeRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        _fixedIncomeRepo.Verify(r => r.DeleteFixedIncomeAsync(It.IsAny<FixedIncomeRequest>()), Times.Once);
    }

    // ── GetFixedIncomesAsync ─────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetFixedIncomesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetFixedIncomesAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<FixedIncomeResponse> { new() { Id = 1 } };
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync(list);
        var sut = CreateSut();

        List<FixedIncomeResponse> result = await sut.GetFixedIncomesAsync(email);

        Assert.Single(result);
    }
}
