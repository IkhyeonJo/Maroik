using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

public class IncomeServiceTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private IncomeService CreateSut() => new(
        _incomeRepo.Object,
        _assetRepo.Object,
        _unitOfWork.Object);

    public IncomeServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssetResponse MakeAsset(string name, decimal amount = 1000m) => new()
    {
        ProductName = name,
        AccountEmail = "user@example.com",
        Amount = amount,
        MonetaryUnit = "KRW",
        Deleted = false
    };

    private static IncomeRequest ValidRequest(string asset = "SavingsAccount") => new()
    {
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        DepositMyAssetProductName = asset,
        Amount = 500m,
        Content = "Salary"
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("InvalidClass", "LaborIncome")]
    [InlineData("RegularIncome", "InvalidSub")]
    [InlineData("IrregularIncome", "FinancialIncome")] // FinancialIncome not valid for Irregular
    [InlineData(null, "LaborIncome")]
    public async Task CreateAsync_ReturnsFail_WhenIncomeClassIsInvalid(string? mainClass, string? subClass)
    {
        var sut = CreateSut();
        var request = new IncomeRequest { MainClass = mainClass, SubClass = subClass, DepositMyAssetProductName = "Any" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "SavingsAccount")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("SavingsAccount"));

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetIsDeleted()
    {
        var asset = MakeAsset("SavingsAccount");
        asset.Deleted = true;
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "SavingsAccount")).ReturnsAsync(asset);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("SavingsAccount"));

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_IncrementsAssetAmount_WhenValid()
    {
        const string email = "user@example.com";
        var asset = MakeAsset("SavingsAccount", amount: 2000m);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "SavingsAccount")).ReturnsAsync(asset);
        _incomeRepo.Setup(r => r.CreateIncomeAsync(It.IsAny<IncomeRequest>())).Returns(Task.CompletedTask);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, ValidRequest("SavingsAccount")); // Amount = 500

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(2500m, captured.Amount); // 2000 + 500
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Theory]
    [InlineData("RegularIncome", "LaborIncome")]
    [InlineData("RegularIncome", "BusinessIncome")]
    [InlineData("RegularIncome", "PensionIncome")]
    [InlineData("RegularIncome", "FinancialIncome")]
    [InlineData("RegularIncome", "RentalIncome")]
    [InlineData("RegularIncome", "OtherIncome")]
    [InlineData("IrregularIncome", "LaborIncome")]
    [InlineData("IrregularIncome", "OtherIncome")]
    public async Task CreateAsync_ReturnsSuccess_ForAllValidIncomeClassCombinations(string mainClass, string subClass)
    {
        const string email = "user@example.com";
        var asset = MakeAsset("SavingsAccount");
        _assetRepo.Setup(r => r.GetAssetAsync(email, "SavingsAccount")).ReturnsAsync(asset);
        _incomeRepo.Setup(r => r.CreateIncomeAsync(It.IsAny<IncomeRequest>())).Returns(Task.CompletedTask);
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new IncomeRequest
        {
            MainClass = mainClass, SubClass = subClass,
            DepositMyAssetProductName = "SavingsAccount", Amount = 100m
        };
        ServiceResult result = await sut.CreateAsync(email, request);

        Assert.True(result.Success);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenIncomeClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new IncomeRequest { Id = 1, MainClass = "BadClass", SubClass = "LaborIncome" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenIncomeRecordNotFound()
    {
        _incomeRepo.Setup(r => r.GetIncomesAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();
        var request = ValidRequest();
        request.Id = 99;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPreviousAssetIsDeleted()
    {
        const string email = "user@example.com";
        var existing = new IncomeResponse
        {
            Id = 1, DepositMyAssetProductName = "OldAsset", Amount = 100m,
            MainClass = "RegularIncome", SubClass = "LaborIncome"
        };
        var deletedAsset = MakeAsset("OldAsset");
        deletedAsset.Deleted = true;
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([existing]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "OldAsset")).ReturnsAsync(deletedAsset);
        var sut = CreateSut();

        var request = ValidRequest("NewAsset");
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_AdjustsSameAssetBalance_WhenAssetNameUnchanged()
    {
        const string email = "user@example.com";
        var existing = new IncomeResponse
        {
            Id = 1, DepositMyAssetProductName = "SavingsAccount", Amount = 300m,
            MainClass = "RegularIncome", SubClass = "LaborIncome"
        };
        var asset = MakeAsset("SavingsAccount", amount: 1300m);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([existing]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "SavingsAccount")).ReturnsAsync(asset);
        _incomeRepo.Setup(r => r.UpdateIncomeAsync(It.IsAny<IncomeRequest>())).Returns(Task.CompletedTask);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        // Same asset, new amount = 500
        var request = new IncomeRequest
        {
            Id = 1, MainClass = "RegularIncome", SubClass = "LaborIncome",
            DepositMyAssetProductName = "SavingsAccount", Amount = 500m
        };
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1500m, captured.Amount); // 1300 - 300 + 500
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdjustsBothAssets_WhenAssetNameChanges()
    {
        const string email = "user@example.com";
        var existing = new IncomeResponse
        {
            Id = 1, DepositMyAssetProductName = "OldAsset", Amount = 200m,
            MainClass = "RegularIncome", SubClass = "LaborIncome"
        };
        var oldAsset = MakeAsset("OldAsset", amount: 500m);
        var newAsset = MakeAsset("NewAsset", amount: 100m);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([existing]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "OldAsset")).ReturnsAsync(oldAsset);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "NewAsset")).ReturnsAsync(newAsset);
        _incomeRepo.Setup(r => r.UpdateIncomeAsync(It.IsAny<IncomeRequest>())).Returns(Task.CompletedTask);

        var updatedAssets = new List<AssetRequest>();
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(updatedAssets.Add)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new IncomeRequest
        {
            Id = 1, MainClass = "RegularIncome", SubClass = "LaborIncome",
            DepositMyAssetProductName = "NewAsset", Amount = 400m
        };
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(300m, updatedAssets.First(a => a.ProductName == "OldAsset").Amount); // 500 - 200
        Assert.Equal(500m, updatedAssets.First(a => a.ProductName == "NewAsset").Amount); // 100 + 400
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenIncomeNotFound()
    {
        _incomeRepo.Setup(r => r.GetIncomesAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenAssetIsDeleted()
    {
        const string email = "user@example.com";
        var income = new IncomeResponse { Id = 1, DepositMyAssetProductName = "SavingsAccount", Amount = 100m };
        var deletedAsset = MakeAsset("SavingsAccount");
        deletedAsset.Deleted = true;
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([income]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "SavingsAccount")).ReturnsAsync(deletedAsset);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_DecrementsAssetAmount_WhenValid()
    {
        const string email = "user@example.com";
        var income = new IncomeResponse { Id = 1, DepositMyAssetProductName = "SavingsAccount", Amount = 250m };
        var asset = MakeAsset("SavingsAccount", amount: 1000m);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([income]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "SavingsAccount")).ReturnsAsync(asset);
        _incomeRepo.Setup(r => r.DeleteIncomeAsync(It.IsAny<IncomeRequest>())).Returns(Task.CompletedTask);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(750m, captured.Amount); // 1000 - 250
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── GetIncomesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetIncomesAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<IncomeResponse> { new() { Id = 1 } };
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync(list);
        var sut = CreateSut();

        List<IncomeResponse> result = await sut.GetIncomesAsync(email);

        Assert.Single(result);
    }
}
