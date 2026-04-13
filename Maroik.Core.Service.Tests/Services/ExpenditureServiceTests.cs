using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ExpenditureService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers expenditure CRUD, asset-balance adjustments on create/update/delete,
/// and category-class validation.
/// </summary>
public class ExpenditureServiceTests
{
    private readonly Mock<IExpenditureRepository> _expenditureRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ExpenditureService CreateSut() => new(
        _expenditureRepo.Object,
        _assetRepo.Object,
        _unitOfWork.Object);

    public ExpenditureServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssetResponse MakeAsset(string name, decimal amount = 1000m, string currency = "KRW") => new()
    {
        ProductName = name,
        AccountEmail = "user@example.com",
        Amount = amount,
        MonetaryUnit = currency,
        Deleted = false
    };

    private static ExpenditureRequest ConsumerRequest(string paymentMethod = "Wallet", decimal amount = 100m) => new()
    {
        MainClass = "ConsumerSpending",
        SubClass = "MealOrEatOutExpenses",
        PaymentMethod = paymentMethod,
        Amount = amount,
        Content = "Lunch"
    };

    private static ExpenditureRequest SavingsTransferRequest(
        string paymentMethod = "Checking",
        string depositAsset = "Savings",
        decimal amount = 200m) => new()
    {
        MainClass = "RegularSavings",
        SubClass = "Deposit",
        PaymentMethod = paymentMethod,
        MyDepositAsset = depositAsset,
        Amount = amount,
        Content = "Monthly savings"
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenMainClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "InvalidClass", SubClass = "Anything" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSubClassIsInvalidForMainClass()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "ConsumerSpending", SubClass = "Deposit" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest("Wallet"));

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetIsDeleted()
    {
        var asset = MakeAsset("Wallet");
        asset.Deleted = true;
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync(asset);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest("Wallet"));

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAndDepositAssetAreSame_ForTransferType()
    {
        _assetRepo.Setup(r => r.GetAssetAsync(It.IsAny<string>(), "Wallet")).ReturnsAsync(MakeAsset("Wallet"));
        var sut = CreateSut();
        var request = SavingsTransferRequest(paymentMethod: "Wallet", depositAsset: "Wallet");

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        Assert.Contains("same", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync((AssetResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetIsDeleted()
    {
        var depositAsset = MakeAsset("Savings");
        depositAsset.Deleted = true;
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(depositAsset);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenCurrenciesMismatch()
    {
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking", currency: "KRW"));
        _assetRepo.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(MakeAsset("Savings", currency: "USD"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        Assert.Contains("MonetaryUnit", result.ErrorKey);
    }

    [Fact]
    public async Task CreateAsync_DeductsPaymentAsset_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.CreateExpenditureAsync(It.IsAny<ExpenditureRequest>())).Returns(Task.CompletedTask);

        AssetRequest? updatedAsset = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => updatedAsset = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, ConsumerRequest("Wallet", 150m));

        Assert.True(result.Success);
        Assert.NotNull(updatedAsset);
        Assert.Equal(850m, updatedAsset.Amount); // 1000 - 150
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DeductsPaymentAndCreditsSavings_ForTransferType()
    {
        const string email = "user@example.com";
        var checking = MakeAsset("Checking", amount: 2000m);
        var savings = MakeAsset("Savings", amount: 500m);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Checking")).ReturnsAsync(checking);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Savings")).ReturnsAsync(savings);
        _expenditureRepo.Setup(r => r.CreateExpenditureAsync(It.IsAny<ExpenditureRequest>())).Returns(Task.CompletedTask);

        var updatedAssets = new List<AssetRequest>();
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(updatedAssets.Add)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, SavingsTransferRequest("Checking", "Savings", 300m));

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1700m, updatedAssets.First(a => a.ProductName == "Checking").Amount);
        Assert.Equal(800m, updatedAssets.First(a => a.ProductName == "Savings").Amount);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassValidationFails()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { Id = 1, MainClass = "BadClass", SubClass = "Anything" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.Setup(r => r.GetExpendituresAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();
        var request = ConsumerRequest();
        request.Id = 99; // non-existent

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPreviousPaymentAssetIsDeleted()
    {
        const string email = "user@example.com";
        var previous = new ExpenditureResponse
        {
            Id = 1, PaymentMethod = "Wallet", MyDepositAsset = "", Amount = 50m,
            MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses"
        };
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([previous]);
        var deletedWallet = MakeAsset("Wallet");
        deletedWallet.Deleted = true;
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(deletedWallet);
        var sut = CreateSut();

        var request = ConsumerRequest("Wallet", 100m);
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_AdjustsAssetBalance_ForConsumerSpendingAmountChange()
    {
        const string email = "user@example.com";
        // Previous expenditure: spent 50 from Wallet
        var previous = new ExpenditureResponse
        {
            Id = 1, PaymentMethod = "Wallet", MyDepositAsset = "", Amount = 50m,
            MainClass = "ConsumerSpending", SubClass = "MealOrEatOutExpenses"
        };
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([previous]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.UpdateExpenditureAsync(It.IsAny<ExpenditureRequest>())).Returns(Task.CompletedTask);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        // New request: spend 100 from Wallet (revert 50, apply 100 → net -50)
        var request = ConsumerRequest("Wallet", 100m);
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(950m, captured.Amount); // 1000 + 50 - 100
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.Setup(r => r.GetExpendituresAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RevertsPaymentAssetBalance_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var expenditure = new ExpenditureResponse
        {
            Id = 1, PaymentMethod = "Wallet", MyDepositAsset = "", Amount = 200m
        };
        var wallet = MakeAsset("Wallet", amount: 800m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([expenditure]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.DeleteExpenditureAsync(It.IsAny<ExpenditureRequest>())).Returns(Task.CompletedTask);

        AssetRequest? captured = null;
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(req => captured = req)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1000m, captured.Amount); // 800 + 200 (reverted)
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RevertsPaymentAndDepositAssets_ForTransferType()
    {
        const string email = "user@example.com";
        var expenditure = new ExpenditureResponse
        {
            Id = 1, PaymentMethod = "Checking", MyDepositAsset = "Savings", Amount = 300m
        };
        var checking = MakeAsset("Checking", amount: 700m);
        var savings = MakeAsset("Savings", amount: 800m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([expenditure]);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Checking")).ReturnsAsync(checking);
        _assetRepo.Setup(r => r.GetAssetAsync(email, "Savings")).ReturnsAsync(savings);
        _expenditureRepo.Setup(r => r.DeleteExpenditureAsync(It.IsAny<ExpenditureRequest>())).Returns(Task.CompletedTask);

        var updatedAssets = new List<AssetRequest>();
        _assetRepo.Setup(r => r.UpdateAssetAsync(It.IsAny<AssetRequest>()))
            .Callback<AssetRequest>(updatedAssets.Add)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1000m, updatedAssets.First(a => a.ProductName == "Checking").Amount); // 700 + 300
        Assert.Equal(500m, updatedAssets.First(a => a.ProductName == "Savings").Amount);   // 800 - 300
    }

    // ── GetExpendituresAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetExpendituresAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<ExpenditureResponse> { new() { Id = 1 } };
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(list);
        var sut = CreateSut();

        List<ExpenditureResponse> result = await sut.GetExpendituresAsync(email);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
