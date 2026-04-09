using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IExpenditureRepository> _expenditureRepo = new();
    private readonly Mock<IFixedExpenditureRepository> _fixedExpenditureRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AssetService CreateSut() => new(
        _assetRepo.Object,
        _expenditureRepo.Object,
        _fixedExpenditureRepo.Object,
        _unitOfWork.Object);

    public AssetServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssetResponse MakeAsset(string name) => new()
    {
        ProductName = name,
        AccountEmail = "user@example.com",
        Item = "FreeDepositAndWithdrawal",
        Amount = 1000m,
        MonetaryUnit = "KRW",
        Deleted = false
    };

    private static AssetRequest ValidRequest(string name = "MyBank") => new()
    {
        ProductName = name,
        Item = "FreeDepositAndWithdrawal",
        Amount = 0m,
        MonetaryUnit = "KRW"
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("InvalidItem")]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateAsync_ReturnsFail_WhenItemIsInvalid(string? item)
    {
        var sut = CreateSut();
        var request = new AssetRequest { ProductName = "MyBank", Item = item };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        Assert.Contains("invalid", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenAssetWithSameNameAlreadyExists()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "MyBank")).ReturnsAsync(MakeAsset("MyBank"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("MyBank"));

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("FreeDepositAndWithdrawal")]
    [InlineData("TrustAsset")]
    [InlineData("CashAsset")]
    [InlineData("SavingsAsset")]
    [InlineData("InvestmentAsset")]
    [InlineData("RealEstate")]
    [InlineData("Movables")]
    [InlineData("OtherPhysicalAsset")]
    [InlineData("InsuranceAsset")]
    public async Task CreateAsync_ReturnsSuccess_ForAllValidItemTypes(string item)
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "MyBank")).ReturnsAsync((AssetResponse?)null);
        _assetRepo.Setup(r => r.CreateAssetAsync(It.IsAny<AssetRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new AssetRequest { ProductName = "MyBank", Item = item };
        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_SetsAccountEmailAndTimestamps_BeforeInserting()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "MyBank")).ReturnsAsync((AssetResponse?)null);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.CreateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.CreateAsync("user@example.com", ValidRequest("MyBank"));

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured.AccountEmail);
        Assert.False(captured.Deleted);
        Assert.True(captured.Created > DateTime.MinValue);
        Assert.True(captured.Updated > DateTime.MinValue);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenItemIsInvalid()
    {
        var sut = CreateSut();
        var request = new AssetRequest { ProductName = "MyBank", Item = "InvalidItem" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, "MyBank");

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenOriginalAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "OldName")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync("user@example.com", ValidRequest("NewName"), "OldName");

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenRenameReturnsZeroRowsAffected()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "OldName")).ReturnsAsync(MakeAsset("OldName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<AssetRequest>(), "OldName")).ReturnsAsync(0);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync("user@example.com", ValidRequest("NewName"), "OldName");

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CascadesRenameToExpenditureAndFixedExpenditure_WhenSuccessful()
    {
        const string email = "user@example.com";
        _assetRepo.Setup(r => r.GetAssetAsync(email, "OldName")).ReturnsAsync(MakeAsset("OldName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<AssetRequest>(), "OldName")).ReturnsAsync(1);
        _fixedExpenditureRepo.Setup(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName")).ReturnsAsync(0);
        _expenditureRepo.Setup(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName")).ReturnsAsync(0);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync(email, ValidRequest("NewName"), "OldName");

        Assert.True(result.Success);
        _fixedExpenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName"), Times.Once);
        _expenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName"), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "MyBank")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", "MyBank");

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_SetsDeletedFlagAndUpdates_WhenAssetFound()
    {
        const string email = "user@example.com";
        _assetRepo.Setup(r => r.GetAssetAsync(email, "MyBank")).ReturnsAsync(MakeAsset("MyBank"));

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, "MyBank");

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.True(captured.Deleted);
    }

    // ── GetAssetsAsync / GetAssetAsync ───────────────────────────────────────

    [Fact]
    public async Task GetAssetsAsync_DelegatesToRepository()
    {
        var list = new List<AssetResponse> { MakeAsset("MyBank") };
        _assetRepo.Setup(r => r.GetAssetsAsync("user@example.com")).ReturnsAsync(list);
        var sut = CreateSut();

        List<AssetResponse> result = await sut.GetAssetsAsync("user@example.com");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAssetAsync_DelegatesToRepository()
    {
        var asset = MakeAsset("MyBank");
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "MyBank")).ReturnsAsync(asset);
        var sut = CreateSut();

        AssetResponse? result = await sut.GetAssetAsync("user@example.com", "MyBank");

        Assert.NotNull(result);
        Assert.Equal("MyBank", result.ProductName);
    }
}
