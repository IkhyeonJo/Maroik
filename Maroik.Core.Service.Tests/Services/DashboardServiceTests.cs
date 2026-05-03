using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DashboardService"/>.
/// All repository dependencies are replaced with Moq mocks.
/// Covers the summary aggregation (per-year / per-year-month income and expenditure
/// breakdowns) and the default monetary unit synchronisation logic.
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IExpenditureRepository> _expenditureRepo = new();
    private readonly Mock<IFixedIncomeRepository> _fixedIncomeRepo = new();
    private readonly Mock<IFixedExpenditureRepository> _fixedExpenditureRepo = new();

    private DashboardService CreateSut() => new(
        _accountRepo.Object,
        _assetRepo.Object,
        _incomeRepo.Object,
        _expenditureRepo.Object,
        _fixedIncomeRepo.Object,
        _fixedExpenditureRepo.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Asset MakeAsset(string name, string unit = "KRW") =>
        Asset.Reconstitute(
            productName: name,
            accountEmail: "user@example.com",
            item: "Deposit",
            amount: 1000m,
            monetaryUnit: unit,
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    private static Account MakeAccount(string email = "user@example.com", string? unit = null) =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "hash",
            nickname: "TestUser",
            avatarImagePath: null,
            role: "User",
            timeZoneIanaId: "UTC",
            defaultMonetaryUnit: unit,
            locked: false,
            loginAttempt: 0,
            emailConfirmed: true,
            agreedServiceTerms: true,
            registrationToken: null,
            resetPasswordToken: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            message: null,
            deleted: false);

    private void SetupEmptyFinancials(string email)
    {
        _incomeRepo.Setup(r => r.GetCurrentYearIncomesAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetCurrentYearExpendituresAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);
        _incomeRepo.Setup(r => r.GetCurrentYearMonthIncomesAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetCurrentYearMonthExpendituresAsync(email, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([]);
    }

    // ── GetSummaryAsync ──────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetSummaryAsync</c> returns expected selected year and month.</summary>
    [Fact]
    public async Task GetSummaryAsync_ReturnsExpectedSelectedYearAndMonth()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        var assets = new List<Asset>();

        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync(assets);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([]);
        var sut = CreateSut();

        DashboardSummary summary = await sut.GetSummaryAsync(email, "2025", "3", "UTC");

        Assert.Equal(2025, summary.SelectedYear);
        Assert.Equal(3, summary.SelectedMonth);
    }

    /// <summary>Verifies that <c>GetSummaryAsync</c> uses current year and month when invalid input given.</summary>
    [Fact]
    public async Task GetSummaryAsync_UsesCurrentYearAndMonth_WhenInvalidInputGiven()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync([]);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([]);
        var sut = CreateSut();

        DashboardSummary summary = await sut.GetSummaryAsync(email, "bad", "bad", "UTC");

        Assert.Equal(DateTime.UtcNow.Year, summary.SelectedYear);
        Assert.Equal(DateTime.UtcNow.Month, summary.SelectedMonth);
    }

    /// <summary>Verifies that <c>GetSummaryAsync</c> sets default monetary unit when account has none but assets exist.</summary>
    [Fact]
    public async Task GetSummaryAsync_SetsDefaultMonetaryUnit_WhenAccountHasNoneButAssetsExist()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        var assets = new List<Asset>
        {
            MakeAsset("Wallet", "KRW"),
            MakeAsset("Checking", "KRW"),
            MakeAsset("Savings", "USD")
        };

        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync(assets);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        SetupEmptyFinancials(email);
        var sut = CreateSut();

        DashboardSummary summary = await sut.GetSummaryAsync(email, "2025", "1", "UTC");

        // Most-used unit is KRW (2 assets)
        Assert.Equal("KRW", summary.DefaultMonetaryUnit);
        _accountRepo.Verify(r => r.UpdateAccountAsync(It.IsAny<Account>()), Times.AtLeastOnce);
    }

    /// <summary>Verifies that <c>GetSummaryAsync</c> clears default monetary unit when no assets exist.</summary>
    [Fact]
    public async Task GetSummaryAsync_ClearsDefaultMonetaryUnit_WhenNoAssetsExist()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: "KRW");
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync([]);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _incomeRepo.Setup(r => r.GetIncomesAsync(email)).ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetExpendituresAsync(email)).ReturnsAsync([]);
        var sut = CreateSut();

        DashboardSummary summary = await sut.GetSummaryAsync(email, "2025", "1", "UTC");

        Assert.Null(summary.DefaultMonetaryUnit);
    }

    // ── GetNoticeCountsAsync ─────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetNoticeCountsAsync</c> returns zero counts when no fixed items.</summary>
    [Fact]
    public async Task GetNoticeCountsAsync_ReturnsZeroCounts_WhenNoFixedItems()
    {
        const string email = "user@example.com";
        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync([]);
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync([]);
        var sut = CreateSut();

        NotificationCounts counts = await sut.GetNoticeCountsAsync(email, 7);

        Assert.Equal(0, counts.FixedIncomesNoticed);
        Assert.Equal(0, counts.FixedExpendituresNoticed);
        Assert.Equal(0, counts.FixedIncomesExpired);
        Assert.Equal(0, counts.FixedExpendituresExpired);
    }

    /// <summary>Verifies that <c>GetNoticeCountsAsync</c> counts expired items when maturity date is past.</summary>
    [Fact]
    public async Task GetNoticeCountsAsync_CountsExpiredItems_WhenMaturityDateIsPast()
    {
        const string email = "user@example.com";
        var pastDate = DateTime.UtcNow.Date.AddDays(-10);

        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync(
        [
            FixedIncome.Reconstitute(
                id: 1,
                accountEmail: email,
                mainClass: "RegularIncome",
                subClass: "LaborIncome",
                content: null,
                amount: 100m,
                monetaryUnit: "KRW",
                depositMyAssetProductName: "Wallet",
                depositMonth: 1,
                depositDay: 1,
                maturityDate: pastDate,
                note: null,
                unpunctuality: false,
                created: DateTime.UtcNow,
                updated: DateTime.UtcNow)
        ]);
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync(
        [
            FixedExpenditure.Reconstitute(
                id: 1,
                accountEmail: email,
                mainClass: "RegularExpenditure",
                subClass: "LivingExpenditure",
                content: null,
                amount: 100m,
                monetaryUnit: "KRW",
                paymentMethod: "Wallet",
                myDepositAsset: null,
                depositMonth: 1,
                depositDay: 1,
                maturityDate: pastDate,
                note: null,
                unpunctuality: false,
                created: DateTime.UtcNow,
                updated: DateTime.UtcNow)
        ]);
        var sut = CreateSut();

        NotificationCounts counts = await sut.GetNoticeCountsAsync(email, 7);

        Assert.Equal(1, counts.FixedIncomesExpired);
        Assert.Equal(1, counts.FixedExpendituresExpired);
    }

    /// <summary>Verifies that <c>GetNoticeCountsAsync</c> counts noticed when unpunctuality is true.</summary>
    [Fact]
    public async Task GetNoticeCountsAsync_CountsNoticed_WhenUnpunctualityIsTrue()
    {
        const string email = "user@example.com";
        var futureDate = DateTime.UtcNow.Date.AddYears(1);

        _fixedIncomeRepo.Setup(r => r.GetFixedIncomesAsync(email)).ReturnsAsync(
        [
            FixedIncome.Reconstitute(
                id: 1,
                accountEmail: email,
                mainClass: "RegularIncome",
                subClass: "LaborIncome",
                content: null,
                amount: 100m,
                monetaryUnit: "KRW",
                depositMyAssetProductName: "Wallet",
                depositMonth: 1,
                depositDay: 1,
                maturityDate: futureDate,
                note: null,
                unpunctuality: true,
                created: DateTime.UtcNow,
                updated: DateTime.UtcNow)
        ]);
        _fixedExpenditureRepo.Setup(r => r.GetFixedExpendituresAsync(email)).ReturnsAsync([]);
        var sut = CreateSut();

        NotificationCounts counts = await sut.GetNoticeCountsAsync(email, 7);

        Assert.Equal(1, counts.FixedIncomesNoticed);
    }

    // ── UpdateDefaultMonetaryUnitAsync ───────────────────────────────────────

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> does nothing when account not found.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_DoesNothing_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("missing@example.com")).ReturnsAsync((Account?)null);
        _assetRepo.Setup(r => r.GetAssetsAsync("missing@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync("missing@example.com", "KRW");

        _accountRepo.Verify(r => r.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> sets unit when unit exists in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsUnit_WhenUnitExistsInAssets()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync([MakeAsset("Wallet", "KRW")]);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(email, "KRW");

        _accountRepo.Verify(r => r.UpdateAccountAsync(It.Is<Account>(acc => acc.DefaultMonetaryUnit == "KRW")), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> sets null when unit not in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsNull_WhenUnitNotInAssets()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: "KRW");
        _assetRepo.Setup(r => r.GetAssetsAsync(email)).ReturnsAsync([MakeAsset("Wallet", "USD")]);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(email)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(email, "KRW"); // KRW not in assets

        _accountRepo.Verify(r => r.UpdateAccountAsync(It.Is<Account>(acc => acc.DefaultMonetaryUnit == null)), Times.Once);
    }

    // ── GetServerResourceSummary ─────────────────────────────────────────────

    /// <summary>Verifies that <c>GetServerResourceSummary</c> returns empty when files do not exist.</summary>
    [Fact]
    public void GetServerResourceSummary_ReturnsEmpty_WhenFilesDoNotExist()
    {
        var sut = CreateSut();

        ServerResourceSummary summary = sut.GetServerResourceSummary(
            "nonexistent_host.txt",
            "nonexistent_docker.txt");

        Assert.Empty(summary.HostCpuInfo);
        Assert.Empty(summary.HostMemoryInfo);
        Assert.Empty(summary.HostDiskInfo);
        Assert.Equal(0.0, summary.HostCpuNumeric);
    }

    /// <summary>Verifies that <c>GetServerResourceSummary</c> parses host cpu numeric when from host file.</summary>
    [Fact]
    public void GetServerResourceSummary_ParsesHostCpuNumeric_FromHostFile()
    {
        string hostFile = Path.GetTempFileName();
        string dockerFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(hostFile,
                "Host CPU Information: 45.5%\nHost Memory Information: Memory: 4GB/8GB\nHost Disk Information: Disk: 100GB/500GB");
            File.WriteAllText(dockerFile, "Docker Resource Usage:\napp 12.3% 100MiB / 512MiB");

            var sut = CreateSut();
            ServerResourceSummary summary = sut.GetServerResourceSummary(hostFile, dockerFile);

            Assert.Equal(45.5, summary.HostCpuNumeric, precision: 1);
        }
        finally
        {
            File.Delete(hostFile);
            File.Delete(dockerFile);
        }
    }
}
