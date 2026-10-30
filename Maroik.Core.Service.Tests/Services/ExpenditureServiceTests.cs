using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ExpenditureService"/>.
/// All repository dependencies are replaced with hand-written fakes.
/// Covers expenditure CRUD, asset-balance adjustments on create/update/delete,
/// and category-class validation.
/// </summary>
public class ExpenditureServiceTests : IAsyncDisposable
{
    /// <summary>Fake <c>IExpenditureRepository</c> injected into the system under test.</summary>
    private readonly FakeExpenditureRepository _expenditureRepo = new();
    /// <summary>Fake <c>IAssetBalanceStore</c> injected into the system under test.</summary>
    private readonly FakeAssetBalanceStore _assetBalance = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<ExpenditureService> _logger = new();

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies.</summary>
    private ExpenditureService CreateSut() => new(
        _expenditureRepo,
        _assetBalance,
        _unitOfWork,
        _logger,
        _time);

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public ExpenditureServiceTests()
    {
        _unitOfWork.AnswerBeginAsync((isolationLevel, _) => isolationLevel == null, (_, _) => Task.CompletedTask);
        _unitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerDisposeAsync(() => ValueTask.CompletedTask);

        // UpdateAsync/DeleteAsync fetch a single record by id; answer that lookup from the list-based
        // setups the tests declare via GetByAccountEmailAsync. UpdateAsync/DeleteAsync use
        // the FOR UPDATE variant — the row lock itself is covered by the repository tests, so here
        // it resolves identically.
        Func<string, long, CancellationToken, Task<Expenditure?>> findById = async (e, id, c) =>
            (await _expenditureRepo.GetByAccountEmailAsync(e, c)).FirstOrDefault(x => x.Id == id);
        _expenditureRepo.AnswerFindByEmailAndIdAsync(findById);
        _expenditureRepo.AnswerFindByEmailAndIdForUpdateAsync(findById);

        // GetAssetsOrdinalAsync fetches every asset in one batched call; answer it from the per-name
        // GetAssetAsync setups the tests below declare.
        _assetBalance.AnswerGetAssetsAsync(async (email, names, ct) => {
                var result = new Dictionary<string, Asset>(StringComparer.Ordinal);
                foreach (string name in names)
                {
                    Asset? asset = await _assetBalance.GetAssetAsync(email, name, ct);
                    if (asset != null) result[name] = asset;
                }
                return result;
            });
    }

    // -- Helpers --------------------------------------------------------------

    /// <summary>A persisted asset named <paramref name="name"/>.</summary>
    private static Asset MakeAsset(string name, decimal amount = 1000m, string currency = "KRW", bool deleted = false) =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", amount, currency, null, deleted, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>A consumer-spending request paid from <paramref name="paymentMethod"/> (no deposit target).</summary>
    private static ExpenditureRequest ConsumerRequest(string paymentMethod = "Wallet", decimal amount = 100m) => new()
    {
        MainClass = "ConsumerSpending",
        SubClass = "MealOrEatOutExpenses",
        PaymentMethod = paymentMethod,
        Amount = amount,
        Content = "Lunch"
    };

    /// <summary>A regular-savings transfer from <paramref name="paymentMethod"/> into <paramref name="depositAsset"/>.</summary>
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

    // -- CreateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when main class is invalid.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenMainClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "InvalidClass", SubClass = "Anything" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when subclass is invalid for main class.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenSubClassIsInvalidForMainClass()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { MainClass = "ConsumerSpending", SubClass = "Deposit" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetNotFound()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment asset is deleted.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAssetIsDeleted()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Wallet", deleted: true)));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when payment and deposit asset are same for transfer type.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenPaymentAndDepositAssetAreSame_ForTransferType()
    {
        _assetBalance.AnswerGetAssetAsync((_, productName, _) => productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Wallet")));
        var sut = CreateSut();
        var request = SavingsTransferRequest(paymentMethod: "Wallet", depositAsset: "Wallet");

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("same", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Checking", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Checking")));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Savings", (_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset is deleted.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetIsDeleted()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Checking", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Checking")));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Savings", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Savings", deleted: true)));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when currencies mismatch.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenCurrenciesMismatch()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Checking", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Checking", currency: "KRW")));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Savings", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Savings", currency: "USD")));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", SavingsTransferRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("MonetaryUnit", result.ErrorKey, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>CreateAsync</c> deducts payment asset for consumer spending.</summary>
    [Fact]
    public async Task CreateAsync_DeductsPaymentAsset_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(wallet));
        _expenditureRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);

        Asset? updatedAsset = null;
        _assetBalance.AnswerSaveAsync((a, _) => { updatedAsset = a; return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, ConsumerRequest("Wallet", 150m), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(updatedAsset);
        Assert.Equal(850m, updatedAsset.Balance.Amount); // 1000 - 150
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> deducts payment and credits savings for transfer type.</summary>
    [Fact]
    public async Task CreateAsync_DeductsPaymentAndCreditsSavings_ForTransferType()
    {
        const string email = "user@example.com";
        var checking = MakeAsset("Checking", amount: 2000m);
        var savings = MakeAsset("Savings", amount: 500m);
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Checking", (_, _, _) => Task.FromResult<Asset?>(checking));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Savings", (_, _, _) => Task.FromResult<Asset?>(savings));
        _expenditureRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);

        var updatedAssets = new List<Asset>();
        _assetBalance.AnswerSaveAsync((a, _) => { updatedAssets.Add(a); return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, SavingsTransferRequest("Checking", "Savings", 300m), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1700m, updatedAssets.First(a => a.ProductName == "Checking").Balance.Amount);
        Assert.Equal(800m, updatedAssets.First(a => a.ProductName == "Savings").Balance.Amount);
    }

    // -- UpdateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when class validation fails.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenClassValidationFails()
    {
        var sut = CreateSut();
        var request = new ExpenditureRequest { Id = 1, MainClass = "BadClass", SubClass = "Anything" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when expenditure not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([]));
        var sut = CreateSut();
        var request = ConsumerRequest();
        request.Id = 99;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when previous payment asset is deleted.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPreviousPaymentAssetIsDeleted()
    {
        const string email = "user@example.com";
        var previous = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            previous
        ]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Wallet", deleted: true)));
        var sut = CreateSut();

        var request = ConsumerRequest();
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that <c>UpdateAsync</c> fails fast with AssetNotFound when the request retargets a
    /// non-transfer expenditure's payment method to an asset name that doesn't exist, instead of
    /// silently persisting the update and only failing later inside AdjustAssetBalancesAsync.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenNewPaymentAssetNotFound()
    {
        const string email = "user@example.com";
        var previous = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            previous
        ]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(MakeAsset("Wallet")));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "GhostWallet", (_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        var request = ConsumerRequest("GhostWallet");
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> adjusts asset balance for consumer spending amount change.</summary>
    [Fact]
    public async Task UpdateAsync_AdjustsAssetBalance_ForConsumerSpendingAmountChange()
    {
        const string email = "user@example.com";
        // Previous expenditure: spent 50 from Wallet
        var previous = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        var wallet = MakeAsset("Wallet", amount: 1000m);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            previous
        ]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(wallet));
        _expenditureRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.AnswerSaveAsync((a, _) => { captured = a; return Task.CompletedTask; });
        var sut = CreateSut();

        // New request: spend 100 from Wallet (revert 50, apply 100 → net -50)
        var request = ConsumerRequest();
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(950m, captured.Balance.Amount); // 1000 + 50 - 100
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> locks touched assets in ordinal name order (not
    /// payment/deposit role order) so concurrent transfer transactions on the same asset pair
    /// cannot deadlock against each other or against <c>CreateAsync</c>.</summary>
    [Fact]
    public async Task UpdateAsync_LocksAssetsInOrdinalOrder_ForTransferType()
    {
        const string email = "user@example.com";
        var previous = Expenditure.Reconstitute(1, email, "RegularSavings", "Deposit", null, 100m, "KRW", "Zebra", "Apple", null, DateTime.UtcNow, DateTime.UtcNow);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            previous
        ]));
        _expenditureRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        var callOrder = new List<string>();
        _assetBalance.AnswerGetAssetAsync((accountEmail, _, _) => accountEmail == email, (_, name, _) => { callOrder.Add(name); return Task.FromResult<Asset?>(MakeAsset(name)); });
        _assetBalance.AnswerSaveAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        // Payment method "Zebra" sorts after deposit asset "Apple"; locking them in
        // payment-then-deposit role order (Zebra, Apple) would break the ordinal lock order.
        var request = SavingsTransferRequest("Zebra", "Apple", 100m);
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(["Apple", "Zebra"], callOrder.Distinct());
    }

    // -- DeleteAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when expenditure not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenExpenditureNotFound()
    {
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([]));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> reverts payment asset balance for consumer spending.</summary>
    [Fact]
    public async Task DeleteAsync_RevertsPaymentAssetBalance_ForConsumerSpending()
    {
        const string email = "user@example.com";
        var expenditure = Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 200m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        var wallet = MakeAsset("Wallet", amount: 800m);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            expenditure
        ]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(wallet));
        _expenditureRepo.AnswerDeleteByIdAsync((_, _) => Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.AnswerSaveAsync((a, _) => { captured = a; return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1000m, captured.Balance.Amount); // 800 + 200 (reverted)
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> reverts payment and deposit assets for transfer type.</summary>
    [Fact]
    public async Task DeleteAsync_RevertsPaymentAndDepositAssets_ForTransferType()
    {
        const string email = "user@example.com";
        var expenditure = Expenditure.Reconstitute(1, email, "RegularSavings", "Deposit", null, 300m, "KRW", "Checking", "Savings", null, DateTime.UtcNow, DateTime.UtcNow);
        var checking = MakeAsset("Checking", amount: 700m);
        var savings = MakeAsset("Savings", amount: 800m);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            expenditure
        ]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Checking", (_, _, _) => Task.FromResult<Asset?>(checking));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "Savings", (_, _, _) => Task.FromResult<Asset?>(savings));
        _expenditureRepo.AnswerDeleteByIdAsync((_, _) => Task.CompletedTask);

        var updatedAssets = new List<Asset>();
        _assetBalance.AnswerSaveAsync((a, _) => { updatedAssets.Add(a); return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(1000m, updatedAssets.First(a => a.ProductName == "Checking").Balance.Amount); // 700 + 300
        Assert.Equal(500m, updatedAssets.First(a => a.ProductName == "Savings").Balance.Amount);   // 800 - 300
    }

    // -- GetExpendituresAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetExpendituresAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetExpendituresAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<Expenditure>
        {
            Expenditure.Reconstitute(1, email, "ConsumerSpending", "MealOrEatOutExpenses", null, 100m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow)
        };
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>(list));
        var sut = CreateSut();

        List<ExpenditureResponse> result = await sut.GetExpendituresAsync(email, TestContext.Current.CancellationToken);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
    }

    // -- SearchExpendituresAsync ------------------------------------------------

    /// <summary>The search is delegated to the repository for the caller's account and its rows are mapped to responses.</summary>
    [Fact]
    public async Task SearchExpendituresAsync_DelegatesToRepository_AndMapsTheRows()
    {
        const string email = "user@example.com";
        _expenditureRepo.AnswerSearchByAccountEmailAsync((accountEmail, search, _) => accountEmail == email && search == "lunch", (_, _, _) => Task.FromResult<List<Expenditure>>([Expenditure.Reconstitute(7, email, "ConsumerSpending", "MealOrEatOutExpenses", "Lunch", 100m, "KRW", "Wallet", "", null, DateTime.UtcNow, DateTime.UtcNow)]));

        List<ExpenditureResponse> result = await CreateSut().SearchExpendituresAsync(email, "lunch", TestContext.Current.CancellationToken);

        Assert.Equal(7, Assert.Single(result).Id);
    }

    // -- CreateAsync: money-affecting failure branches -------------------------------

    /// <summary>Owner e-mail the asset lookups are keyed on.</summary>
    private const string Email = "user@example.com";

    /// <summary>Makes the asset lookup return each of <paramref name="assets"/> by product name.</summary>
    private void GivenAssets(params Asset[] assets)
    {
        foreach (Asset a in assets)
            _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == Email && productName == a.ProductName, (_, _, _) => Task.FromResult<Asset?>(a));
    }

    /// <summary>Makes the owner's expenditure list contain only <paramref name="previous"/>.</summary>
    private void GivenExistingExpenditure(Expenditure previous) =>
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([previous]));

    /// <summary>A persisted consumer-spending expenditure (id 1) paid from <paramref name="payment"/>.</summary>
    private static Expenditure ExistingConsumer(decimal amount = 50m, string payment = "Wallet") =>
        Expenditure.Reconstitute(1, Email, "ConsumerSpending", "MealOrEatOutExpenses", null, amount, "KRW", payment, "", null, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>A persisted regular-savings transfer (id 1) from <paramref name="payment"/> into <paramref name="deposit"/>.</summary>
    private static Expenditure ExistingTransfer(decimal amount = 50m, string payment = "Checking", string deposit = "Savings") =>
        Expenditure.Reconstitute(1, Email, "RegularSavings", "Deposit", null, amount, "KRW", payment, deposit, null, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>A negative amount typed by the user is stored (and withdrawn) as its absolute value.</summary>
    [Fact]
    public async Task CreateAsync_StoresAndWithdrawsTheAbsoluteAmount_WhenTheRequestAmountIsNegative()
    {
        GivenAssets(MakeAsset("Wallet", amount: 1000m));
        Expenditure? stored = null;
        _expenditureRepo.AnswerCreateAsync((e, _) => { stored = e; return Task.CompletedTask; });
        Asset? saved = null;
        _assetBalance.AnswerSaveAsync((a, _) => { saved = a; return Task.CompletedTask; });

        ServiceResult result = await CreateSut().CreateAsync(Email, ConsumerRequest("Wallet", -250m), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(250m, stored?.Amount.Amount);
        Assert.Equal(750m, saved?.Balance.Amount);
    }

    /// <summary>A withdrawal that would push the balance past the column range is refused and nothing is committed.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheRangeError_AndRollsBack_WhenTheWithdrawalWouldOverflowTheBalance()
    {
        GivenAssets(MakeAsset("Wallet", amount: -FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().CreateAsync(Email, ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Finance.AmountOutOfRange", result.ErrorCode);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A transfer whose deposit would overflow the destination balance is refused after the source was debited in memory — nothing is saved.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheRangeError_AndSavesNoDepositAsset_WhenTheDepositWouldOverflow()
    {
        GivenAssets(MakeAsset("Checking", amount: 1000m), MakeAsset("Savings", amount: FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().CreateAsync(Email, SavingsTransferRequest(amount: 200m), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Finance.AmountOutOfRange", result.ErrorCode);
        Assert.DoesNotContain(_assetBalance.SaveAsyncCalls, call => (call.Asset.ProductName == "Savings"));
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A record the domain rejects (over-long content) is returned as its validation error before any balance moves.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheDomainValidationError_BeforeMovingAnyBalance()
    {
        GivenAssets(MakeAsset("Wallet"));
        ExpenditureRequest request = ConsumerRequest();
        request.Content = new string('x', 100_000);

        ServiceResult result = await CreateSut().CreateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.Equal("Finance.ContentTooLong", result.ErrorCode);
        Assert.Empty(_expenditureRepo.CreateAsyncCalls);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
    }

    /// <summary>An unexpected failure on create is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task CreateAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenAssets(MakeAsset("Wallet"));
        _expenditureRepo.AnswerCreateAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().CreateAsync(Email, ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    // -- UpdateAsync: money-affecting failure branches --------------------------------

    /// <summary>Sets <paramref name="r"/>'s id to 1 (an update request) and returns it.</summary>
    private static ExpenditureRequest WithId(ExpenditureRequest r) { r.Id = 1; return r; }

    /// <summary>A transfer edited so that both sides name the same asset is refused before any lookup or lock.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPaymentAndDepositAssetAreTheSame_ForTransferType()
    {
        GivenExistingExpenditure(ExistingTransfer());

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest("Checking", "Checking")), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.SameAsset", result.ErrorCode);
        Assert.Empty(_assetBalance.GetAssetAsyncCalls);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A transfer retargeted to a deposit asset that does not exist is refused, and the row is not updated.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetNotFound_WhenTheNewDepositAssetDoesNotExist()
    {
        GivenExistingExpenditure(ExistingTransfer());
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest("Checking", "Ghost")), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetNotFound", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A transfer retargeted onto a since-archived deposit asset is refused (its balance must never be touched).</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetDeleted_WhenTheNewDepositAssetIsArchived()
    {
        GivenExistingExpenditure(ExistingTransfer());
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"), MakeAsset("Archived", deleted: true));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest("Checking", "Archived")), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetDeleted", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A transfer retargeted to a deposit asset in another currency is refused.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsCurrencyMismatch_WhenTheNewAssetsUseDifferentCurrencies()
    {
        GivenExistingExpenditure(ExistingTransfer());
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"), MakeAsset("UsdSavings", currency: "USD"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest("Checking", "UsdSavings")), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.CurrencyMismatch", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>
    /// An edit is refused in the same order as a new expenditure (AssetUsagePolicy): a missing payment asset is
    /// reported before an archived deposit asset, not the other way round.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReportsAMissingPaymentAsset_BeforeAnArchivedDepositAsset()
    {
        GivenExistingExpenditure(ExistingTransfer());
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"), MakeAsset("Archived", deleted: true));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest("Unknown", "Archived")), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetNotFound", result.ErrorCode);
        Assert.Equal(ServiceErrorKeys.SelectedAssetNotFound, result.ErrorKey);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A record whose ORIGINAL deposit asset has since been archived cannot be edited (its old balance impact cannot be reverted).</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetDeleted_WhenThePreviousDepositAssetIsArchived()
    {
        GivenExistingExpenditure(ExistingTransfer(deposit: "OldSavings"));
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"), MakeAsset("OldSavings", deleted: true));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest()), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetDeleted", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A record edited to content the domain rejects is returned as its validation error and not persisted.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsTheDomainValidationError_AndDoesNotPersist()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));
        ExpenditureRequest request = WithId(ConsumerRequest());
        request.Content = new string('x', 100_000);

        ServiceResult result = await CreateSut().UpdateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.Equal("Finance.ContentTooLong", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A balance adjustment the asset refuses (range overflow) rolls the whole update back instead of committing a half-applied edit.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsTheAdjustmentError_AndRollsBack_WhenTheBalanceWouldOverflow()
    {
        GivenExistingExpenditure(ExistingConsumer(amount: 50m));
        GivenAssets(MakeAsset("Wallet", amount: -FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest()), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An unexpected failure on update is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task UpdateAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));
        _expenditureRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest("Wallet", 60m)), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    // -- DeleteAsync: failure branches ------------------------------------------------

    /// <summary>A transfer whose deposit asset has since been archived cannot be deleted (its balance must not be touched).</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsAssetDeleted_WhenTheDepositAssetIsArchived()
    {
        GivenExistingExpenditure(ExistingTransfer());
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings", deleted: true));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetDeleted", result.ErrorCode);
        Assert.Empty(_expenditureRepo.DeleteByIdAsyncCalls);
    }

    /// <summary>Reverting the record's impact must keep every balance in range; otherwise the delete is refused and rolled back.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsTheAdjustmentError_AndKeepsTheRow_WhenRevertingWouldOverflowTheBalance()
    {
        GivenExistingExpenditure(ExistingConsumer(amount: 100m));
        GivenAssets(MakeAsset("Wallet", amount: FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_expenditureRepo.DeleteByIdAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unexpected failure on delete is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task DeleteAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));
        _expenditureRepo.AnswerDeleteByIdAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A record whose ORIGINAL payment asset has since been archived cannot be moved to another asset (its old balance impact cannot be reverted).</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetDeleted_WhenThePreviousPaymentAssetIsArchived_AndTheNewOneIsNot()
    {
        var previous = Expenditure.Reconstitute(1, Email, "ConsumerSpending", "MealOrEatOutExpenses", null, 50m, "KRW", "OldWallet", "", null, DateTime.UtcNow, DateTime.UtcNow);
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([
            previous
        ]));
        GivenAssets(MakeAsset("Wallet"), MakeAsset("OldWallet", deleted: true));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest()), TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetDeleted", result.ErrorCode);
        Assert.Empty(_expenditureRepo.UpdateEntityAsyncCalls);
    }

    // -- Transaction boundaries, result codes, transfer routing and failure logging -------

    /// <summary>Asserts the one Error entry names <see cref="Email"/>, carries the thrown exception and starts with <paramref name="prefix"/>.</summary>
    private void AssertLoggedFailure(string prefix, Exception thrown)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(thrown, record.Exception);
        Assert.StartsWith(prefix, record.Message, StringComparison.Ordinal);
        Assert.Contains(Email, record.Message, StringComparison.Ordinal);
    }

    /// <summary>Records every asset passed to <c>SaveAsync</c>, keyed by name with its saved balance.</summary>
    private Dictionary<string, decimal> CaptureSavedBalances()
    {
        var saved = new Dictionary<string, decimal>();
        _assetBalance.AnswerSaveAsync((a, _) => { saved[a.ProductName] = a.Balance.Amount; return Task.CompletedTask; });
        return saved;
    }

    /// <summary>A stored expenditure is returned mapped, looked up under the caller's account.</summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsTheMappedExpenditure_WhenItExists()
    {
        GivenExistingExpenditure(ExistingConsumer(amount: 42m));

        ExpenditureResponse? result = await CreateSut().GetByIdAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(42m, result.Amount);
    }

    /// <summary>An id the caller does not own resolves to null.</summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenTheExpenditureDoesNotExist()
    {
        GivenExistingExpenditure(ExistingConsumer());

        Assert.Null(await CreateSut().GetByIdAsync(Email, 2, TestContext.Current.CancellationToken));
    }

    /// <summary>A successful create opens the transaction and commits it, without rolling back.</summary>
    [Fact]
    public async Task CreateAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenAssets(MakeAsset("Wallet"));

        ServiceResult result = await CreateSut().CreateAsync(Email, ConsumerRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown payment asset is a NotFound with the localizable "asset not found" message.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsAssetNotFound_WithItsMessage_WhenThePaymentAssetIsUnknown()
    {
        ServiceResult result = await CreateSut().CreateAsync(Email, ConsumerRequest("Ghost"), TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Expenditure.AssetNotFound", result.ErrorCode);
        Assert.Equal("The selected asset could not be found.", result.ErrorKey);
    }

    /// <summary>
    /// A consumer expenditure ignores a deposit asset the form still carries: the record keeps no deposit asset, that
    /// asset is neither locked nor credited, and only the payment asset is withdrawn.
    /// </summary>
    [Fact]
    public async Task CreateAsync_IgnoresTheDepositAsset_ForANonTransferExpenditure()
    {
        GivenAssets(MakeAsset("Wallet", amount: 1000m), MakeAsset("Savings", amount: 500m));
        Expenditure? stored = null;
        _expenditureRepo.AnswerCreateAsync((e, _) => { stored = e; return Task.CompletedTask; });
        Dictionary<string, decimal> saved = CaptureSavedBalances();
        ExpenditureRequest request = ConsumerRequest(amount: 100m);
        request.MyDepositAsset = "Savings";

        ServiceResult result = await CreateSut().CreateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(string.IsNullOrEmpty(stored?.MyDepositAsset));
        Assert.Equal(new Dictionary<string, decimal> { ["Wallet"] = 900m }, saved);
        Assert.DoesNotContain(_assetBalance.GetAssetsAsyncCalls, call => call.AccountEmail == Email && (call.ProductNames.Contains("Savings")));
    }

    /// <summary>A transfer is stored with its deposit asset.</summary>
    [Fact]
    public async Task CreateAsync_StoresTheDepositAsset_ForATransfer()
    {
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"));
        Expenditure? stored = null;
        _expenditureRepo.AnswerCreateAsync((e, _) => { stored = e; return Task.CompletedTask; });

        ServiceResult result = await CreateSut().CreateAsync(Email, SavingsTransferRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("Savings", stored?.MyDepositAsset);
    }

    /// <summary>A create that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task CreateAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenAssets(MakeAsset("Wallet"));
        var thrown = new InvalidOperationException("boom");
        _expenditureRepo.AnswerCreateAsync((_, _) => Task.FromException(thrown));

        await CreateSut().CreateAsync(Email, ConsumerRequest(), TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to create expenditure", thrown);
    }

    /// <summary>A successful update opens the transaction and commits it.</summary>
    [Fact]
    public async Task UpdateAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest()), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown expenditure id is a NotFound with the localizable "record not found" message.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsExpenditureNotFound_WithItsMessage_WhenTheRecordIsUnknown()
    {
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([]));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest()), TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Expenditure.NotFound", result.ErrorCode);
        Assert.Equal("The expenditure record could not be found.", result.ErrorKey);
    }

    /// <summary>
    /// Editing a transfer reverts the old movement and applies the new one on both sides: the payment asset gets the
    /// old amount back and loses the new one, the deposit asset loses the old amount and gains the new one.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_MovesTheNewAmountIntoTheDepositAsset_ForATransfer()
    {
        GivenExistingExpenditure(ExistingTransfer(amount: 50m));
        GivenAssets(MakeAsset("Checking", amount: 1000m), MakeAsset("Savings", amount: 1000m));
        Dictionary<string, decimal> saved = CaptureSavedBalances();

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest(amount: 200m)), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(850m, saved["Checking"]);  // 1000 + 50 - 200
        Assert.Equal(1150m, saved["Savings"]);  // 1000 - 50 + 200
    }

    /// <summary>
    /// A consumer expenditure edit ignores a deposit asset the form still carries: the record keeps no deposit asset
    /// and that asset is neither locked nor credited.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_IgnoresTheDepositAsset_ForANonTransferExpenditure()
    {
        GivenExistingExpenditure(ExistingConsumer(amount: 50m));
        GivenAssets(MakeAsset("Wallet", amount: 1000m), MakeAsset("Savings", amount: 500m));
        Expenditure? stored = null;
        _expenditureRepo.AnswerUpdateEntityAsync((e, _) => { stored = e; return Task.CompletedTask; });
        Dictionary<string, decimal> saved = CaptureSavedBalances();
        ExpenditureRequest request = WithId(ConsumerRequest(amount: 100m));
        request.MyDepositAsset = "Savings";

        ServiceResult result = await CreateSut().UpdateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(string.IsNullOrEmpty(stored?.MyDepositAsset));
        Assert.Equal(new Dictionary<string, decimal> { ["Wallet"] = 950m }, saved);
        Assert.DoesNotContain(_assetBalance.GetAssetsAsyncCalls, call => call.AccountEmail == Email && (call.ProductNames.Contains("Savings")));
    }

    /// <summary>A transfer edit is stored with its deposit asset.</summary>
    [Fact]
    public async Task UpdateAsync_StoresTheDepositAsset_ForATransfer()
    {
        GivenExistingExpenditure(ExistingConsumer(payment: "Checking"));
        GivenAssets(MakeAsset("Checking"), MakeAsset("Savings"));
        Expenditure? stored = null;
        _expenditureRepo.AnswerUpdateEntityAsync((e, _) => { stored = e; return Task.CompletedTask; });

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(SavingsTransferRequest()), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("Savings", stored?.MyDepositAsset);
    }

    /// <summary>An update that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task UpdateAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));
        var thrown = new InvalidOperationException("boom");
        _expenditureRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(thrown));

        await CreateSut().UpdateAsync(Email, WithId(ConsumerRequest()), TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to update expenditure 1", thrown);
    }

    /// <summary>A successful delete opens the transaction and commits it.</summary>
    [Fact]
    public async Task DeleteAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown expenditure id on delete is a NotFound with the localizable "record not found" message.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsExpenditureNotFound_WithItsMessage_WhenTheRecordIsUnknown()
    {
        _expenditureRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Expenditure>>([]));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Expenditure.NotFound", result.ErrorCode);
        Assert.Equal("The expenditure record could not be found.", result.ErrorKey);
    }

    /// <summary>An expenditure paid from a since-archived asset cannot be deleted (the archived balance must not move).</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsAssetDeleted_WhenThePaymentAssetIsArchived()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet", deleted: true));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal("Expenditure.AssetDeleted", result.ErrorCode);
        Assert.Empty(_expenditureRepo.DeleteByIdAsyncCalls);
    }

    /// <summary>A delete that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task DeleteAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenExistingExpenditure(ExistingConsumer());
        GivenAssets(MakeAsset("Wallet"));
        var thrown = new InvalidOperationException("boom");
        _expenditureRepo.AnswerDeleteByIdAsync((_, _) => Task.FromException(thrown));

        await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to delete expenditure 1", thrown);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
