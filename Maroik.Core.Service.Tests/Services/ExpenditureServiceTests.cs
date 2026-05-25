using Maroik.Core.Contract.Domains.Finance;
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
    private readonly Mock<IAssetBalanceDomainService> _assetBalance = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ExpenditureService CreateSut() => new(
        _expenditureRepo.Object,
        _assetBalance.Object,
        _unitOfWork.Object);

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public ExpenditureServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Asset MakeAsset(string name, decimal amount = 1000m, string currency = "KRW", bool deleted = false) =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", amount, currency, null, deleted, DateTime.UtcNow, DateTime.UtcNow);

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

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when main class is invalid.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenMainClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "InvalidClass", SubClass = "Anything" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when sub class is invalid for main class.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSubClassIsInvalidForMainClass()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "ConsumerSpending", SubClass = "Deposit" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetNotFound()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync((Asset?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest("Wallet"));

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment asset is deleted.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetIsDeleted()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Wallet")).ReturnsAsync(MakeAsset("Wallet", deleted: true));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest("Wallet"));

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment and deposit asset are same for transfer type.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAndDepositAssetAreSame_ForTransferType()
    {
        _assetBalance.Setup(r => r.GetAssetAsync(It.IsAny<string>(), "Wallet")).ReturnsAsync(MakeAsset("Wallet"));
        var sut = CreateSut();
        var request = SavingsTransferRequest(paymentMethod: "Wallet", depositAsset: "Wallet");

        ServiceResult result = await sut.CreateAsync("user@example.com", request);

        Assert.False(result.Success);
        Assert.Contains("same", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync((Asset?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset is deleted.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetIsDeleted()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(MakeAsset("Savings", deleted: true));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when currencies mismatch.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenCurrenciesMismatch()
    {
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Checking")).ReturnsAsync(MakeAsset("Checking", currency: "KRW"));
        _assetBalance.Setup(r => r.GetAssetAsync("user@example.com", "Savings")).ReturnsAsync(MakeAsset("Savings", currency: "USD"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest());

        Assert.False(result.Success);
        Assert.Contains("MonetaryUnit", result.ErrorKey);
    }

    /// <summary>Verifies that <c>CreateAsync</c> deducts payment asset for consumer spending.</summary>
    [Fact]
    public async Task CreateAsync_DeductsPaymentAsset_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.CreateExpenditureAsync(It.IsAny<Expenditure>())).Returns(Task.CompletedTask);

        Asset? updatedAsset = null;
        _assetBalance.Setup(r => r.SaveAsync(It.IsAny<Asset>()))
            .Callback<Asset>(a => updatedAsset = a)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, ConsumerRequest("Wallet", 150m));

        Assert.True(result.Success);
        Assert.NotNull(updatedAsset);
        Assert.Equal(850m, updatedAsset.Balance.Amount); // 1000 - 150
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateAsync</c> deducts payment and credits savings for transfer type.</summary>
    [Fact]
    public async Task CreateAsync_DeductsPaymentAndCreditsSavings_ForTransferType()
    {
        const string email = "user@example.com";
        var checking = MakeAsset("Checking", amount: 2000m);
        var savings = MakeAsset("Savings", amount: 500m);
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Checking")).ReturnsAsync(checking);
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Savings")).ReturnsAsync(savings);
        _expenditureRepo.Setup(r => r.CreateExpenditureAsync(It.IsAny<Expenditure>())).Returns(Task.CompletedTask);

        var updatedAssets = new List<Asset>();
        _assetBalance.Setup(r => r.SaveAsync(It.IsAny<Asset>()))
            .Callback<Asset>(updatedAssets.Add)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, SavingsTransferRequest("Checking", "Savings", 300m));

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1700m, updatedAssets.First(a => a.ProductName == "Checking").Balance.Amount);
        Assert.Equal(800m, updatedAssets.First(a => a.ProductName == "Savings").Balance.Amount);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when class validation fails.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassValidationFails()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { Id = 1, MainClass = "BadClass", SubClass = "Anything" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when expenditure not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.Setup(r => r.GetExpendituresAsync("user@example.com")).ReturnsAsync(new List<Expenditure>());
        var sut = CreateSut();
        var request = ConsumerRequest();
        request.Id = 99;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when previous payment asset is deleted.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPreviousPaymentAssetIsDeleted()
    {
        const string email = "user@example.com";
        var previous = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(new List<Expenditure> { previous });
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(MakeAsset("Wallet", deleted: true));
        var sut = CreateSut();

        var request = ConsumerRequest("Wallet", 100m);
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> adjusts asset balance for consumer spending amount change.</summary>
    [Fact]
    public async Task UpdateAsync_AdjustsAssetBalance_ForConsumerSpendingAmountChange()
    {
        const string email = "user@example.com";
        // Previous expenditure: spent 50 from Wallet
        var previous = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(new List<Expenditure> { previous });
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.UpdateExpenditureAsync(It.IsAny<Expenditure>())).Returns(Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.Setup(r => r.SaveAsync(It.IsAny<Asset>()))
            .Callback<Asset>(a => captured = a)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        // New request: spend 100 from Wallet (revert 50, apply 100 → net -50)
        var request = ConsumerRequest("Wallet", 100m);
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(950m, captured.Balance.Amount); // 1000 + 50 - 100
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when expenditure not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.Setup(r => r.GetExpendituresAsync("user@example.com")).ReturnsAsync(new List<Expenditure>());
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> reverts payment asset balance for consumer spending.</summary>
    [Fact]
    public async Task DeleteAsync_RevertsPaymentAssetBalance_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var expenditure = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 200m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        var wallet = MakeAsset("Wallet", amount: 800m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(new List<Expenditure> { expenditure });
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Wallet")).ReturnsAsync(wallet);
        _expenditureRepo.Setup(r => r.DeleteExpenditureAsync(It.IsAny<Expenditure>())).Returns(Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.Setup(r => r.SaveAsync(It.IsAny<Asset>()))
            .Callback<Asset>(a => captured = a)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1000m, captured.Balance.Amount); // 800 + 200 (reverted)
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> reverts payment and deposit assets for transfer type.</summary>
    [Fact]
    public async Task DeleteAsync_RevertsPaymentAndDepositAssets_ForTransferType()
    {
        const string email = "user@example.com";
        var expenditure = Expenditure.Reconstitute(1, email, "RegularSavings", "Deposit", null, 300m, "KRW", "Checking", "Savings", null, DateTime.UtcNow, DateTime.UtcNow);
        var checking = MakeAsset("Checking", amount: 700m);
        var savings = MakeAsset("Savings", amount: 800m);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(new List<Expenditure> { expenditure });
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Checking")).ReturnsAsync(checking);
        _assetBalance.Setup(r => r.GetAssetAsync(email, "Savings")).ReturnsAsync(savings);
        _expenditureRepo.Setup(r => r.DeleteExpenditureAsync(It.IsAny<Expenditure>())).Returns(Task.CompletedTask);

        var updatedAssets = new List<Asset>();
        _assetBalance.Setup(r => r.SaveAsync(It.IsAny<Asset>()))
            .Callback<Asset>(updatedAssets.Add)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1000m, updatedAssets.First(a => a.ProductName == "Checking").Balance.Amount); // 700 + 300
        Assert.Equal(500m, updatedAssets.First(a => a.ProductName == "Savings").Balance.Amount);   // 800 - 300
    }

    // ── GetExpendituresAsync ─────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetExpendituresAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetExpendituresAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<Expenditure>
        {
            Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 100m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow)
        };
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync(list);
        var sut = CreateSut();

        List<ExpenditureResponse> result = await sut.GetExpendituresAsync(email);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
