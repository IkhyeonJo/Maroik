using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AssetService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers asset CRUD, balance adjustments triggered by income/expenditure changes,
/// and product-name rename propagation.
/// </summary>
public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IExpenditureRepository> _expenditureRepo = new();
    private readonly Mock<IFixedExpenditureRepository> _fixedExpenditureRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AssetService CreateSut() => new(_assetRepo.Object, _expenditureRepo.Object, _fixedExpenditureRepo.Object, _unitOfWork.Object, NullLogger<AssetService>.Instance);

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public AssetServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // -- Helpers --------------------------------------------------------------

    private static Asset MakeAsset(string name, decimal amount = 1000m, string currency = "KRW", bool deleted = false) =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", amount, currency, null, deleted, DateTime.UtcNow, DateTime.UtcNow);

    private void SetupFindAsset(string productName, Asset? asset)
    {
        _assetRepo.Setup(r => r.FindByEmailAndProductNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        // UpdateAsync row-locks its read via the ForUpdate variant (see AssetService.UpdateAsync);
        // stub it too so Update-path tests using this same helper still find the asset.
        _assetRepo.Setup(r => r.FindByEmailAndProductNameForUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
    }

    private static AssetRequest ValidRequest(string name = "MyBank") => new()
    {
        ProductName = name,
        Item = "FreeDepositAndWithdrawal",
        Amount = 0m,
        MonetaryUnit = "KRW"
    };

    // -- CreateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when item is invalid.</summary>
    [Theory]
    [InlineData("InvalidItem")]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateAsync_ReturnsFail_WhenItemIsInvalid(string? item)
    {
        var sut = CreateSut();
        var request = new AssetRequest { ProductName = "MyBank", Item = item };

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("invalid", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when asset with same name already exists.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenAssetWithSameNameAlreadyExists()
    {
        SetupFindAsset("MyBank", MakeAsset("MyBank"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("MyBank"), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that <c>CreateAsync</c> rejects a name that belongs to a soft-deleted asset rather
    /// than reviving it — a freed-up-looking name must never be reusable, since reviving the row
    /// would silently re-attach its old transaction history to what the user believes is a new,
    /// unrelated asset.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSameNameIsSoftDeleted()
    {
        SetupFindAsset("MyBank", MakeAsset("MyBank", amount: 500m, currency: "KRW", deleted: true));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("MyBank"), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _assetRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
        _assetRepo.Verify(r => r.CreateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for all valid item types.</summary>
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
        SetupFindAsset("MyBank", null);
        _assetRepo.Setup(r => r.CreateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new AssetRequest { ProductName = "MyBank", Item = item, MonetaryUnit = "KRW" };
        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>
    /// Regression test: FindAssetAsync + CreateAsync is a read-then-write race (two concurrent
    /// requests for the same product name can both pass the prior existence check). The DB's
    /// primary key on (ProductName, AccountEmail) still rejects the second insert — verify that
    /// failure is translated to the same friendly "already exists" result instead of propagating
    /// the raw DB exception to the caller.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenConcurrentInsertViolatesUniqueConstraint()
    {
        SetupFindAsset("MyBank", null);
        var pgException = new Exception("duplicate key value violates unique constraint \"Asset_pk\" (23505)");
        _assetRepo.Setup(r => r.CreateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("insert failed", pgException));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest("MyBank"), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> sets account email and timestamps before inserting.</summary>
    [Fact]
    public async Task CreateAsync_SetsAccountEmailAndTimestamps_BeforeInserting()
    {
        SetupFindAsset("MyBank", null);

        Asset? captured = null;
        _assetRepo.Setup(r => r.CreateAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
            .Callback<Asset, CancellationToken>((asset, _) => captured = asset)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.CreateAsync("user@example.com", ValidRequest("MyBank"), TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured.AccountEmail.Value);
        Assert.False(captured.Deleted);
        Assert.True(captured.Created > DateTime.MinValue);
        Assert.True(captured.Updated > DateTime.MinValue);
    }

    // -- UpdateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when item is invalid.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenItemIsInvalid()
    {
        var sut = CreateSut();
        var request = new AssetRequest { ProductName = "MyBank", Item = "InvalidItem" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, "MyBank", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when original asset not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenOriginalAssetNotFound()
    {
        SetupFindAsset("OldName", null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync("user@example.com", ValidRequest("NewName"), "OldName", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when rename returns zero rows affected.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenRenameReturnsZeroRowsAffected()
    {
        SetupFindAsset("OldName", MakeAsset("OldName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<Asset>(), "OldName", It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync("user@example.com", ValidRequest("NewName"), "OldName", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> commits the rename when successful.</summary>
    [Fact]
    public async Task UpdateAsync_Commits_WhenSuccessful()
    {
        const string email = "user@example.com";
        SetupFindAsset("OldName", MakeAsset("OldName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<Asset>(), "OldName", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync(email, ValidRequest("NewName"), "OldName", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Regression test: renaming an asset to a product name that already exists for this account
    /// hits the same (ProductName, AccountEmail) primary key CreateAsync guards against. Verify
    /// that failure is translated to the same friendly "already exists" result instead of the
    /// generic fallback message.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenRenameViolatesUniqueConstraint()
    {
        SetupFindAsset("OldName", MakeAsset("OldName"));
        var pgException = new Exception("duplicate key value violates unique constraint \"Asset_pk\" (23505)");
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<Asset>(), "OldName", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rename failed", pgException));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync("user@example.com", ValidRequest("NewName"), "OldName", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that a rename cascades to expenditure/fixed-expenditure records that reference the
    /// asset via <c>MyDepositAsset</c> — a column with no foreign key (it also holds an empty string
    /// for transfer-less entries), so the application must propagate the rename itself.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CascadesMyDepositAssetRename_WhenProductNameChanges()
    {
        const string email = "user@example.com";
        SetupFindAsset("OldName", MakeAsset("OldName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<Asset>(), "OldName", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync(email, ValidRequest("NewName"), "OldName", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _expenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName", It.IsAny<CancellationToken>()), Times.Once);
        _fixedExpenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(email, "OldName", "NewName", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that no MyDepositAsset cascade runs when the product name is unchanged.</summary>
    [Fact]
    public async Task UpdateAsync_DoesNotCascadeMyDepositAssetRename_WhenProductNameUnchanged()
    {
        const string email = "user@example.com";
        SetupFindAsset("SameName", MakeAsset("SameName"));
        _assetRepo.Setup(r => r.UpdateAssetWithProductNameAsync(It.IsAny<Asset>(), "SameName", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAsync(email, ValidRequest("SameName"), "SameName", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _expenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fixedExpenditureRepo.Verify(r => r.UpdateMyDepositAssetWithProductNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- DeleteAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when asset not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenAssetNotFound()
    {
        SetupFindAsset("MyBank", null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", "MyBank", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> sets deleted flag and updates when asset found.</summary>
    [Fact]
    public async Task DeleteAsync_SetsDeletedFlagAndUpdates_WhenAssetFound()
    {
        const string email = "user@example.com";
        SetupFindAsset("MyBank", MakeAsset("MyBank"));

        Asset? captured = null;
        _assetRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
            .Callback<Asset, CancellationToken>((asset, _) => captured = asset)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, "MyBank", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.True(captured.Deleted);
    }

    // -- GetAssetsAsync / GetAssetAsync ---------------------------------------

    /// <summary>Verifies that <c>GetAssetsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAssetsAsync_DelegatesToRepository()
    {
        var list = new List<Asset> { MakeAsset("MyBank") };
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var sut = CreateSut();

        List<AssetResponse> result = await sut.GetAssetsAsync("user@example.com", TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    /// <summary>Verifies that <c>GetAssetAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAssetAsync_DelegatesToRepository()
    {
        var asset = MakeAsset("MyBank");
        SetupFindAsset("MyBank", asset);
        var sut = CreateSut();

        AssetResponse? result = await sut.GetAssetAsync("user@example.com", "MyBank", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("MyBank", result.ProductName);
    }
}
