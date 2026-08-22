using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Finance;
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

    // -- Helpers --------------------------------------------------------------

    private static Asset MakeAsset(string name, string unit = "KRW", bool deleted = false) =>
        Asset.Reconstitute(
            productName: name,
            accountEmail: "user@example.com",
            item: "Deposit",
            amount: 1000m,
            monetaryUnit: unit,
            note: null,
            deleted: deleted,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    private static Account MakeAccount(string email = "user@example.com", string? unit = null) =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "hash",
            nickname: "TestUser",
            avatarImagePath: null,
            role: Role.User,
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
        _incomeRepo.Setup(r => r.GetByAccountEmailAndDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _expenditureRepo.Setup(r => r.GetByAccountEmailAndDateRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _incomeRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Income?)null);
        _expenditureRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expenditure?)null);
    }

    // -- GetSummaryAsync ------------------------------------------------------

    /// <summary>Verifies that <c>GetSummaryAsync</c> returns expected selected year and month.</summary>
    [Fact]
    public async Task GetSummaryAsync_ReturnsExpectedSelectedYearAndMonth()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        var assets = new List<Asset>();

        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        _incomeRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);
        _expenditureRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expenditure?)null);
        var sut = CreateSut();

        DashboardDto summary = await sut.GetSummaryAsync(email, "2025", "3", "UTC", TestContext.Current.CancellationToken);

        Assert.Equal(2025, summary.SelectedYear);
        Assert.Equal(3, summary.SelectedMonth);
    }

    /// <summary>Verifies that <c>GetSummaryAsync</c> uses current year and month when invalid input given.</summary>
    [Fact]
    public async Task GetSummaryAsync_UsesCurrentYearAndMonth_WhenInvalidInputGiven()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _incomeRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);
        _expenditureRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expenditure?)null);
        var sut = CreateSut();

        DashboardDto summary = await sut.GetSummaryAsync(email, "bad", "bad", "UTC", TestContext.Current.CancellationToken);

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

        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        SetupEmptyFinancials(email);
        var sut = CreateSut();

        DashboardDto summary = await sut.GetSummaryAsync(email, "2025", "1", "UTC", TestContext.Current.CancellationToken);

        // Most-used unit is KRW (2 assets) — computed in-memory, no DB write
        Assert.Equal("KRW", summary.DefaultMonetaryUnit);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>GetSummaryAsync</c> clears default monetary unit when no assets exist.</summary>
    [Fact]
    public async Task GetSummaryAsync_ClearsDefaultMonetaryUnit_WhenNoAssetsExist()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: "KRW");
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _incomeRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);
        _expenditureRepo.Setup(r => r.GetFirstByAccountEmailOrderedByCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expenditure?)null);
        var sut = CreateSut();

        DashboardDto summary = await sut.GetSummaryAsync(email, "2025", "1", "UTC", TestContext.Current.CancellationToken);

        Assert.Null(summary.DefaultMonetaryUnit);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a soft-deleted asset's currency does not appear in the dashboard's asset
    /// list, monetary-unit options, or effective-currency calculation — even though
    /// GetByAccountEmailAsync itself returns deleted assets (the account-book management
    /// grid needs to see them for restoration).
    /// </summary>
    [Fact]
    public async Task GetSummaryAsync_ExcludesDeletedAssets_FromAssetsAndMonetaryUnits()
    {
        const string email = "user@example.com";
        var account = MakeAccount(unit: null);
        var assets = new List<Asset>
        {
            MakeAsset("Wallet", "KRW"),
            MakeAsset("OldForeignAccount", "USD", deleted: true)
        };

        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _assetRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        SetupEmptyFinancials(email);
        var sut = CreateSut();

        DashboardDto summary = await sut.GetSummaryAsync(email, "2025", "1", "UTC", TestContext.Current.CancellationToken);

        Assert.Single(summary.Assets);
        Assert.DoesNotContain("USD", summary.MonetaryUnits);
        Assert.Equal("KRW", summary.DefaultMonetaryUnit);
    }

    // -- GetNoticeCountsAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetNoticeCountsAsync</c> returns zero counts when no fixed items.</summary>
    [Fact]
    public async Task GetNoticeCountsAsync_ReturnsZeroCounts_WhenNoFixedItems()
    {
        const string email = "user@example.com";
        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = CreateSut();

        NotificationDto counts = await sut.GetNoticeCountsAsync(email, 7, "UTC", TestContext.Current.CancellationToken);

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

        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
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
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
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

        NotificationDto counts = await sut.GetNoticeCountsAsync(email, 7, "UTC", TestContext.Current.CancellationToken);

        Assert.Equal(1, counts.FixedIncomesExpired);
        Assert.Equal(1, counts.FixedExpendituresExpired);
    }

    /// <summary>Verifies that <c>GetNoticeCountsAsync</c> counts noticed when unpunctuality is true.</summary>
    [Fact]
    public async Task GetNoticeCountsAsync_CountsNoticed_WhenUnpunctualityIsTrue()
    {
        const string email = "user@example.com";
        var futureDate = DateTime.UtcNow.Date.AddYears(1);

        _fixedIncomeRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
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
        _fixedExpenditureRepo.Setup(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = CreateSut();

        NotificationDto counts = await sut.GetNoticeCountsAsync(email, 7, "UTC", TestContext.Current.CancellationToken);

        Assert.Equal(1, counts.FixedIncomesNoticed);
    }

    // -- GetServerResourceSummary ---------------------------------------------

    /// <summary>Verifies that <c>GetServerResourceSummary</c> returns empty when files do not exist.</summary>
    [Fact]
    public void GetServerResourceSummary_ReturnsEmpty_WhenFilesDoNotExist()
    {
        var sut = CreateSut();

        ServerResourceDto summary = sut.GetServerResourceSummary(
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
            ServerResourceDto summary = sut.GetServerResourceSummary(hostFile, dockerFile);

            Assert.Equal(45.5, summary.HostCpuNumeric, precision: 1);
        }
        finally
        {
            File.Delete(hostFile);
            File.Delete(dockerFile);
        }
    }

    /// <summary>Verifies that <c>GetServerResourceSummary</c> parses each docker container row into a typed <see cref="DockerContainerResourceDto"/>.</summary>
    [Fact]
    public void GetServerResourceSummary_ParsesDockerContainers_IntoTypedDtos()
    {
        string hostFile = Path.GetTempFileName();
        string dockerFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(hostFile, "Host CPU Information: 1%\nHost Memory Information: Memory: 1GB/2GB\nHost Disk Information: Disk: 1GB/2GB");
            File.WriteAllText(dockerFile,
                "Docker Resource Usage:\n" +
                "NAME CPU% MEM USED / LIMIT\n" +
                "web 12.3% 100MiB / 512MiB\n" +
                "db 4.5% 1GiB / 2GiB");

            var sut = CreateSut();
            ServerResourceDto summary = sut.GetServerResourceSummary(hostFile, dockerFile);

            Assert.Equal(2, summary.DockerContainerResult.Count);
            Assert.Equal("web", summary.DockerContainerResult[0].Name);
            Assert.Equal("12.3%", summary.DockerContainerResult[0].CpuPercent);
            Assert.Equal("100MiB / 512MiB", summary.DockerContainerResult[0].MemUsageDisplay);
            Assert.Equal("db", summary.DockerContainerResult[1].Name);
        }
        finally
        {
            File.Delete(hostFile);
            File.Delete(dockerFile);
        }
    }

    /// <summary>Verifies that <c>GetServerResourceSummary</c> returns an empty (not null) docker container list when the docker file is missing.</summary>
    [Fact]
    public void GetServerResourceSummary_ReturnsEmptyDockerContainerList_WhenDockerFileMissing()
    {
        var sut = CreateSut();

        ServerResourceDto summary = sut.GetServerResourceSummary("nonexistent_host.txt", "nonexistent_docker.txt");

        Assert.Empty(summary.DockerContainerResult);
    }
}
