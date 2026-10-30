using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="IncomeService"/>.
/// All repository dependencies are replaced with hand-written fakes.
/// Covers income CRUD, asset-balance adjustments on create/update/delete,
/// and category-class validation.
/// </summary>
public class IncomeServiceTests : IAsyncDisposable
{
    /// <summary>Fake <c>IIncomeRepository</c> injected into the system under test.</summary>
    private readonly FakeIncomeRepository _incomeRepo = new();
    /// <summary>Fake <c>IAssetBalanceStore</c> injected into the system under test.</summary>
    private readonly FakeAssetBalanceStore _assetBalance = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<IncomeService> _logger = new();

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies.</summary>
    private IncomeService CreateSut() => new(
        _incomeRepo,
        _assetBalance,
        _unitOfWork,
        _logger,
        _time);

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public IncomeServiceTests()
    {
        _unitOfWork.AnswerBeginAsync((isolationLevel, _) => isolationLevel == null, (_, _) => Task.CompletedTask);
        _unitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerDisposeAsync(() => ValueTask.CompletedTask);

        // UpdateAsync/DeleteAsync fetch a single record by id; answer that lookup from the list-based
        // setups the tests declare via GetByAccountEmailAsync. UpdateAsync/DeleteAsync use
        // the FOR UPDATE variant — the row lock itself is covered by the repository tests, so here
        // it resolves identically.
        Func<string, long, CancellationToken, Task<Income?>> findById = async (e, id, c) =>
            (await _incomeRepo.GetByAccountEmailAsync(e, c)).FirstOrDefault(x => x.Id == id);
        _incomeRepo.AnswerFindByEmailAndIdAsync(findById);
        _incomeRepo.AnswerFindByEmailAndIdForUpdateAsync(findById);

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
    private static Asset MakeAsset(string name, decimal amount = 1000m, bool deleted = false, string currency = "KRW")
        => Asset.Reconstitute(
            productName: name,
            accountEmail: "user@example.com",
            item: "Deposit",
            amount: amount,
            monetaryUnit: currency,
            note: null,
            deleted: deleted,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    /// <summary>A valid income request deposited into <paramref name="asset"/>.</summary>
    private static IncomeRequest ValidRequest(string asset = "SavingsAccount") => new()
    {
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        DepositMyAssetProductName = asset,
        Amount = 500m,
        Content = "Salary"
    };

    /// <summary>A persisted income deposited into <paramref name="depositAsset"/>.</summary>
    private static Income MakeIncome(long id, string depositAsset = "SavingsAccount", decimal amount = 100m,
        string mainClass = "RegularIncome", string subClass = "LaborIncome")
        => Income.Reconstitute(
            id: id,
            accountEmail: "user@example.com",
            mainClass: mainClass,
            subClass: subClass,
            content: "Salary",
            amount: amount,
            monetaryUnit: "KRW",
            depositMyAssetProductName: depositAsset,
            note: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    // -- CreateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when income class is invalid.</summary>
    [Theory]
    [InlineData("InvalidClass", "LaborIncome")]
    [InlineData("RegularIncome", "InvalidSub")]
    [InlineData("IrregularIncome", "FinancialIncome")] // FinancialIncome not valid for Irregular
    [InlineData(null, "LaborIncome")]
    public async Task CreateAsync_ReturnsFail_WhenIncomeClassIsInvalid(string? mainClass, string? subClass)
    {
        var sut = CreateSut();
        var request = new IncomeRequest { MainClass = mainClass, SubClass = subClass, DepositMyAssetProductName = "Any" };

        ServiceResult result = await sut.CreateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset not found.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetNotFound()
    {
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns fail when deposit asset is deleted.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenDepositAssetIsDeleted()
    {
        var asset = MakeAsset("SavingsAccount", deleted: true);
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(asset));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync("user@example.com", ValidRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> increments asset amount when valid.</summary>
    [Fact]
    public async Task CreateAsync_IncrementsAssetAmount_WhenValid()
    {
        const string email = "user@example.com";
        var asset = MakeAsset("SavingsAccount", amount: 2000m);
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(asset));
        _incomeRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.AnswerSaveAsync((a, _) => { captured = a; return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAsync(email, ValidRequest(), TestContext.Current.CancellationToken); // Amount = 500

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(2500m, captured.Balance.Amount); // 2000 + 500
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>CreateAsync</c> returns success for all valid income class combinations.</summary>
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
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(asset));
        _incomeRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _assetBalance.AnswerSaveAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        var request = new IncomeRequest
        {
            MainClass = mainClass,
            SubClass = subClass,
            Content = "Monthly income",
            DepositMyAssetProductName = "SavingsAccount",
            Amount = 100m
        };
        ServiceResult result = await sut.CreateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    // -- UpdateAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when income class is invalid.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenIncomeClassIsInvalid()
    {
        var sut = CreateSut();
        var request = new IncomeRequest { Id = 1, MainClass = "BadClass", SubClass = "LaborIncome" };

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when income record not found.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenIncomeRecordNotFound()
    {
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([]));
        var sut = CreateSut();
        var request = ValidRequest();
        request.Id = 99;

        ServiceResult result = await sut.UpdateAsync("user@example.com", request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> returns fail when previous asset is deleted.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenPreviousAssetIsDeleted()
    {
        const string email = "user@example.com";
        var existing = MakeIncome(1, "OldAsset");
        var deletedAsset = MakeAsset("OldAsset", deleted: true);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([existing]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "OldAsset", (_, _, _) => Task.FromResult<Asset?>(deletedAsset));
        var sut = CreateSut();

        var request = ValidRequest("NewAsset");
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> adjusts same asset balance when asset name unchanged.</summary>
    [Fact]
    public async Task UpdateAsync_AdjustsSameAssetBalance_WhenAssetNameUnchanged()
    {
        const string email = "user@example.com";
        var existing = MakeIncome(1, "SavingsAccount", 300m);
        var asset = MakeAsset("SavingsAccount", amount: 1300m);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([existing]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(asset));
        _incomeRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.AnswerSaveAsync((a, _) => { captured = a; return Task.CompletedTask; });
        var sut = CreateSut();

        // Same asset, new amount = 500
        var request = new IncomeRequest
        {
            Id = 1,
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Monthly income",
            DepositMyAssetProductName = "SavingsAccount",
            Amount = 500m
        };
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1500m, captured.Balance.Amount); // 1300 - 300 + 500
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAsync</c> adjusts both assets when asset name changes.</summary>
    [Fact]
    public async Task UpdateAsync_AdjustsBothAssets_WhenAssetNameChanges()
    {
        const string email = "user@example.com";
        var existing = MakeIncome(1, "OldAsset", 200m);
        var oldAsset = MakeAsset("OldAsset", amount: 500m);
        var newAsset = MakeAsset("NewAsset", amount: 100m);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([existing]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "OldAsset", (_, _, _) => Task.FromResult<Asset?>(oldAsset));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "NewAsset", (_, _, _) => Task.FromResult<Asset?>(newAsset));
        _incomeRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        var updatedAssets = new List<Asset>();
        _assetBalance.AnswerSaveAsync((a, _) => { updatedAssets.Add(a); return Task.CompletedTask; });
        var sut = CreateSut();

        var request = new IncomeRequest
        {
            Id = 1,
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Monthly income",
            DepositMyAssetProductName = "NewAsset",
            Amount = 400m
        };
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(2, updatedAssets.Count);
        Assert.Equal(300m, updatedAssets.First(a => a.ProductName == "OldAsset").Balance.Amount); // 500 - 200
        Assert.Equal(500m, updatedAssets.First(a => a.ProductName == "NewAsset").Balance.Amount); // 100 + 400
    }

    /// <summary>Verifies that <c>UpdateAsync</c> locks the previous/new deposit assets in ordinal
    /// name order (not previous-then-new role order) so concurrent transactions on the same
    /// asset pair, including in <see cref="ExpenditureService"/>, cannot deadlock.</summary>
    [Fact]
    public async Task UpdateAsync_LocksAssetsInOrdinalOrder_WhenAssetNameChanges()
    {
        const string email = "user@example.com";
        var existing = MakeIncome(1, depositAsset: "Zebra", amount: 200m);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([existing]));
        _incomeRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        var callOrder = new List<string>();
        _assetBalance.AnswerGetAssetAsync((accountEmail, _, _) => accountEmail == email, (_, name, _) => { callOrder.Add(name); return Task.FromResult<Asset?>(MakeAsset(name)); });
        _assetBalance.AnswerSaveAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        // Previous asset "Zebra" sorts after the new asset "Apple"; locking them in
        // previous-then-new role order (Zebra, Apple) would break the ordinal lock order.
        var request = ValidRequest("Apple");
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(["Apple", "Zebra"], callOrder.Distinct());
    }

    /// <summary>Verifies that <c>UpdateAsync</c> fails and never withdraws from the old asset when
    /// the new deposit asset name doesn't resolve to an owned asset — regression test for a bug
    /// where the withdrawal from the old asset ran unconditionally while the deposit into the
    /// unresolved new asset was silently skipped, losing money.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsFail_WhenNewAssetNotFound_AndAssetNameChanges()
    {
        const string email = "user@example.com";
        var existing = MakeIncome(1, "OldAsset", 200m);
        var oldAsset = MakeAsset("OldAsset", amount: 500m);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([existing]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "OldAsset", (_, _, _) => Task.FromResult<Asset?>(oldAsset));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "GhostAsset", (_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        var request = ValidRequest("GhostAsset");
        request.Id = 1;
        ServiceResult result = await sut.UpdateAsync(email, request, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    // -- DeleteAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when income not found.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenIncomeNotFound()
    {
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([]));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync("user@example.com", 99, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> returns fail when asset is deleted.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFail_WhenAssetIsDeleted()
    {
        const string email = "user@example.com";
        var income = MakeIncome(1);
        var deletedAsset = MakeAsset("SavingsAccount", deleted: true);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([income]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(deletedAsset));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteAsync</c> decrements asset amount when valid.</summary>
    [Fact]
    public async Task DeleteAsync_DecrementsAssetAmount_WhenValid()
    {
        const string email = "user@example.com";
        var income = MakeIncome(1, "SavingsAccount", 250m);
        var asset = MakeAsset("SavingsAccount", amount: 1000m);
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([income]));
        _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == email && productName == "SavingsAccount", (_, _, _) => Task.FromResult<Asset?>(asset));
        _incomeRepo.AnswerDeleteByIdAsync((_, _) => Task.CompletedTask);

        Asset? captured = null;
        _assetBalance.AnswerSaveAsync((a, _) => { captured = a; return Task.CompletedTask; });
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAsync(email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(750m, captured.Balance.Amount); // 1000 - 250
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    // -- GetIncomesAsync ------------------------------------------------------

    /// <summary>Verifies that <c>GetIncomesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetIncomesAsync_DelegatesToRepository()
    {
        const string email = "user@example.com";
        var list = new List<Income> { MakeIncome(1) };
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>(list));
        var sut = CreateSut();

        List<IncomeResponse> result = await sut.GetIncomesAsync(email, TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    // -- SearchIncomesAsync -----------------------------------------------------

    /// <summary>The search is delegated to the repository for the caller's account and its rows are mapped to responses.</summary>
    [Fact]
    public async Task SearchIncomesAsync_DelegatesToRepository_AndMapsTheRows()
    {
        _incomeRepo.AnswerSearchByAccountEmailAsync((accountEmail, search, _) => accountEmail == "user@example.com" && search == "salary", (_, _, _) => Task.FromResult<List<Income>>([MakeIncome(7)]));

        List<IncomeResponse> result = await CreateSut().SearchIncomesAsync("user@example.com", "salary", TestContext.Current.CancellationToken);

        Assert.Equal(7, Assert.Single(result).Id);
    }

    // -- Money-affecting failure branches ------------------------------------------

    /// <summary>Owner e-mail the asset lookups are keyed on.</summary>
    private const string Email = "user@example.com";

    /// <summary>Makes the asset lookup return each of <paramref name="assets"/> by product name.</summary>
    private void GivenAssets(params Asset[] assets)
    {
        foreach (Asset a in assets)
            _assetBalance.AnswerGetAssetAsync((accountEmail, productName, _) => accountEmail == Email && productName == a.ProductName, (_, _, _) => Task.FromResult<Asset?>(a));
    }

    /// <summary>Makes the owner's income list contain only <paramref name="previous"/>.</summary>
    private void GivenExistingIncome(Income previous) =>
        _incomeRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Income>>([previous]));

    /// <summary>Sets <paramref name="r"/>'s id to 1 (an update request) and returns it.</summary>
    private static IncomeRequest WithId(IncomeRequest r) { r.Id = 1; return r; }

    /// <summary>A negative amount typed by the user is stored (and deposited) as its absolute value.</summary>
    [Fact]
    public async Task CreateAsync_StoresAndDepositsTheAbsoluteAmount_WhenTheRequestAmountIsNegative()
    {
        GivenAssets(MakeAsset("SavingsAccount", amount: 1000m));
        Income? stored = null;
        _incomeRepo.AnswerCreateAsync((i, _) => { stored = i; return Task.CompletedTask; });
        Asset? saved = null;
        _assetBalance.AnswerSaveAsync((a, _) => { saved = a; return Task.CompletedTask; });
        IncomeRequest request = ValidRequest();
        request.Amount = -250m;

        ServiceResult result = await CreateSut().CreateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(250m, stored?.Amount.Amount);
        Assert.Equal(1250m, saved?.Balance.Amount);
    }

    /// <summary>A deposit that would push the balance past the column range is refused and nothing is committed.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheRangeError_AndRollsBack_WhenTheDepositWouldOverflowTheBalance()
    {
        GivenAssets(MakeAsset("SavingsAccount", amount: FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().CreateAsync(Email, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Finance.AmountOutOfRange", result.ErrorCode);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An amount with a fifth decimal place is refused (never rounded); nothing is written or committed.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheDecimalPlacesError_AndRollsBack_WhenTheAmountHasFiveDecimals()
    {
        GivenAssets(MakeAsset("SavingsAccount"));
        IncomeRequest request = ValidRequest();
        request.Amount = 0.00005m;

        ServiceResult result = await CreateSut().CreateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.Equal("Finance.AmountTooManyDecimals", result.ErrorCode);
        Assert.Equal("Amount can have up to {0} decimal places.", result.ErrorKey);
        Assert.Equal([4], result.ErrorArgs);
        Assert.Empty(_incomeRepo.CreateAsyncCalls);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A record the domain rejects (over-long content) is returned as its validation error before any balance moves.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsTheDomainValidationError_BeforeMovingAnyBalance()
    {
        GivenAssets(MakeAsset("SavingsAccount"));
        IncomeRequest request = ValidRequest();
        request.Content = new string('x', 100_000);

        ServiceResult result = await CreateSut().CreateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.Equal("Finance.ContentTooLong", result.ErrorCode);
        Assert.Empty(_incomeRepo.CreateAsyncCalls);
        Assert.Empty(_assetBalance.SaveAsyncCalls);
    }

    /// <summary>An unexpected failure on create is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task CreateAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenAssets(MakeAsset("SavingsAccount"));
        _incomeRepo.AnswerCreateAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().CreateAsync(Email, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.Equal("Income.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An income retargeted onto a since-archived asset is refused (its balance must never be touched).</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetDeleted_WhenTheNewAssetIsArchived()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"), MakeAsset("Archived", deleted: true));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest("Archived")), TestContext.Current.CancellationToken);

        Assert.Equal("Income.AssetDeleted", result.ErrorCode);
        Assert.Empty(_incomeRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A record edited to content the domain rejects is returned as its validation error and not persisted.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsTheDomainValidationError_AndDoesNotPersist()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));
        IncomeRequest request = WithId(ValidRequest());
        request.Content = new string('x', 100_000);

        ServiceResult result = await CreateSut().UpdateAsync(Email, request, TestContext.Current.CancellationToken);

        Assert.Equal("Finance.ContentTooLong", result.ErrorCode);
        Assert.Empty(_incomeRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A balance adjustment the asset refuses (range overflow) rolls the whole update back.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsTheAdjustmentError_AndRollsBack_WhenTheBalanceWouldOverflow()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount", amount: FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest()), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An unexpected failure on update is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task UpdateAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));
        _incomeRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest()), TestContext.Current.CancellationToken);

        Assert.Equal("Income.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Reverting the record's impact must keep the balance in range; otherwise the delete is refused and rolled back.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsTheAdjustmentError_AndKeepsTheRow_WhenRevertingWouldOverflowTheBalance()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount", amount: -FinanceAmountPolicy.MaxAbsoluteAmount));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_incomeRepo.DeleteByIdAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unexpected failure on delete is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task DeleteAsync_RollsBackAndReportsUnexpectedFailure_WhenThePersistenceThrows()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));
        _incomeRepo.AnswerDeleteByIdAsync((_, _) => Task.FromException(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal("Income.Unexpected", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    // -- Transaction boundaries, result codes and failure logging --------------------

    /// <summary>Asserts the one Error entry names <see cref="Email"/>, carries the thrown exception and starts with <paramref name="prefix"/>.</summary>
    private void AssertLoggedFailure(string prefix, Exception thrown)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(thrown, record.Exception);
        Assert.StartsWith(prefix, record.Message, StringComparison.Ordinal);
        Assert.Contains(Email, record.Message, StringComparison.Ordinal);
    }

    /// <summary>A stored income is returned mapped, looked up under the caller's account.</summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsTheMappedIncome_WhenItExists()
    {
        GivenExistingIncome(MakeIncome(5, amount: 42m));

        IncomeResponse? result = await CreateSut().GetByIdAsync(Email, 5, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal(42m, result.Amount);
    }

    /// <summary>An id the caller does not own resolves to null.</summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenTheIncomeDoesNotExist()
    {
        GivenExistingIncome(MakeIncome(5));

        Assert.Null(await CreateSut().GetByIdAsync(Email, 6, TestContext.Current.CancellationToken));
    }

    /// <summary>A successful create opens the transaction and commits it, without rolling back.</summary>
    [Fact]
    public async Task CreateAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenAssets(MakeAsset("SavingsAccount"));

        ServiceResult result = await CreateSut().CreateAsync(Email, ValidRequest(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown deposit asset is a NotFound with the localizable "asset not found" message.</summary>
    [Fact]
    public async Task CreateAsync_ReturnsAssetNotFound_WithItsMessage_WhenTheDepositAssetIsUnknown()
    {
        ServiceResult result = await CreateSut().CreateAsync(Email, ValidRequest("Ghost"), TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Income.AssetNotFound", result.ErrorCode);
        Assert.Equal("The selected asset could not be found.", result.ErrorKey);
    }

    /// <summary>A create that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task CreateAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenAssets(MakeAsset("SavingsAccount"));
        var thrown = new InvalidOperationException("boom");
        _incomeRepo.AnswerCreateAsync((_, _) => Task.FromException(thrown));

        await CreateSut().CreateAsync(Email, ValidRequest(), TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to create income", thrown);
    }

    /// <summary>A successful update opens the transaction and commits it.</summary>
    [Fact]
    public async Task UpdateAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest()), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown income id is a NotFound with the localizable "record not found" message.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsIncomeNotFound_WithItsMessage_WhenTheRecordIsUnknown()
    {
        GivenExistingIncome(MakeIncome(2));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest()), TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Income.NotFound", result.ErrorCode);
        Assert.Equal("The income record could not be found.", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Retargeting onto an unknown asset is a NotFound with the localizable "asset not found" message.</summary>
    [Fact]
    public async Task UpdateAsync_ReturnsAssetNotFound_WithItsMessage_WhenTheNewAssetIsUnknown()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest("Ghost")), TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Income.AssetNotFound", result.ErrorCode);
        Assert.Equal("The selected asset could not be found.", result.ErrorKey);
    }

    /// <summary>
    /// An update that clears the deposit asset is the domain's "deposit asset empty" validation error — not an
    /// "asset not found", which only applies to a named asset that does not resolve.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReturnsTheDomainDepositAssetEmptyError_WhenTheRequestClearsTheAsset()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest("")), TestContext.Current.CancellationToken);

        Assert.Equal("Income.DepositAssetEmpty", result.ErrorCode);
        Assert.Empty(_incomeRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>An income moved onto an asset in another currency is re-recorded in the new asset's currency.</summary>
    [Fact]
    public async Task UpdateAsync_RecordsTheNewAssetsCurrency_WhenTheIncomeMovesToAnAssetInAnotherCurrency()
    {
        GivenExistingIncome(MakeIncome(1, "SavingsAccount", 100m));
        GivenAssets(MakeAsset("SavingsAccount"), MakeAsset("DollarAccount", amount: 0m, currency: "USD"));
        Income? stored = null;
        _incomeRepo.AnswerUpdateEntityAsync((i, _) => { stored = i; return Task.CompletedTask; });

        ServiceResult result = await CreateSut().UpdateAsync(Email, WithId(ValidRequest("DollarAccount")), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("USD", stored?.Amount.Currency.Value);
    }

    /// <summary>An update that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task UpdateAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));
        var thrown = new InvalidOperationException("boom");
        _incomeRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(thrown));

        await CreateSut().UpdateAsync(Email, WithId(ValidRequest()), TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to update income 1", thrown);
    }

    /// <summary>A successful delete opens the transaction and commits it.</summary>
    [Fact]
    public async Task DeleteAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown income id on delete is a NotFound with the localizable "record not found" message.</summary>
    [Fact]
    public async Task DeleteAsync_ReturnsIncomeNotFound_WithItsMessage_WhenTheRecordIsUnknown()
    {
        GivenExistingIncome(MakeIncome(2));

        ServiceResult result = await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Income.NotFound", result.ErrorCode);
        Assert.Equal("The income record could not be found.", result.ErrorKey);
    }

    /// <summary>A delete that throws logs the failure with the exception and the account.</summary>
    [Fact]
    public async Task DeleteAsync_LogsTheFailureWithTheException_WhenThePersistenceThrows()
    {
        GivenExistingIncome(MakeIncome(1));
        GivenAssets(MakeAsset("SavingsAccount"));
        var thrown = new InvalidOperationException("boom");
        _incomeRepo.AnswerDeleteByIdAsync((_, _) => Task.FromException(thrown));

        await CreateSut().DeleteAsync(Email, 1, TestContext.Current.CancellationToken);

        AssertLoggedFailure("Failed to delete income 1", thrown);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
