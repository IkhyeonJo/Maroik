using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IDashboardService"/> that aggregates data from multiple repositories
/// to populate the user dashboard and notification badge counts.
/// Calculates yearly/monthly income and expenditure totals grouped by MainClass/SubClass,
/// and computes notification counts for fixed-income and fixed-expenditure entries
/// whose deposit dates are approaching or have passed maturity.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IFixedIncomeRepository _fixedIncomeRepository;
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;

    /// <summary>Initializes a new instance of <see cref="DashboardService"/> with the supplied dependencies.</summary>
    public DashboardService(
        IAccountRepository accountRepository,
        IAssetRepository assetRepository,
        IIncomeRepository incomeRepository,
        IExpenditureRepository expenditureRepository,
        IFixedIncomeRepository fixedIncomeRepository,
        IFixedExpenditureRepository fixedExpenditureRepository)
    {
        _accountRepository = accountRepository;
        _assetRepository = assetRepository;
        _incomeRepository = incomeRepository;
        _expenditureRepository = expenditureRepository;
        _fixedIncomeRepository = fixedIncomeRepository;
        _fixedExpenditureRepository = fixedExpenditureRepository;
    }

    /// <inheritdoc />
    public async Task<DashboardSummary> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId)
    {
        // Normalize year/month
        if (!int.TryParse(year, out int yearInt)) yearInt = DateTime.UtcNow.Year;
        if (!int.TryParse(month, out int monthInt)) monthInt = DateTime.UtcNow.Month;
        string normalizedYear = yearInt.ToString();
        string normalizedMonth = monthInt.ToString("00");
        string normalizedYearMonth = $"{normalizedYear}-{normalizedMonth}";

        var assets = (await _assetRepository.GetAssetsAsync(accountEmail)) ?? [];
        var account = await _accountRepository.GetAccountByEmailAsync(accountEmail);

        // Sync DefaultMonetaryUnit
        await SyncDefaultMonetaryUnitInternalAsync(account, assets);

        var summary = new DashboardSummary
        {
            DefaultMonetaryUnit = account?.DefaultMonetaryUnit,
            Assets = assets,
            SelectedYear = yearInt,
            SelectedMonth = monthInt,
            EndYear = DateTime.UtcNow.Year,
            MonetaryUnits = assets.Select(x => x.MonetaryUnit).Distinct()
        };

        if (summary.DefaultMonetaryUnit != null)
        {
            var yearIncomes = await _incomeRepository.GetCurrentYearIncomesAsync(accountEmail, normalizedYear, timeZoneId);
            var yearExpenditures = await _expenditureRepository.GetCurrentYearExpendituresAsync(accountEmail, normalizedYear, timeZoneId);
            var ymIncomes = await _incomeRepository.GetCurrentYearMonthIncomesAsync(accountEmail, normalizedYearMonth, timeZoneId);
            var ymExpenditures = await _expenditureRepository.GetCurrentYearMonthExpendituresAsync(accountEmail, normalizedYearMonth, timeZoneId);

            // Filter by default monetary unit via asset lookup
            string defaultUnit = summary.DefaultMonetaryUnit;
            summary.YearIncomes = yearIncomes
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.MonetaryUnit == defaultUnit)
                .ToList();
            summary.YearExpenditures = yearExpenditures
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.MonetaryUnit == defaultUnit)
                .ToList();
            summary.YearMonthIncomes = ymIncomes
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.MonetaryUnit == defaultUnit)
                .ToList();
            summary.YearMonthExpenditures = ymExpenditures
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.MonetaryUnit == defaultUnit)
                .ToList();
        }

        // Compute StartYear
        var firstIncome = (await _incomeRepository.GetIncomesAsync(accountEmail))
            .OrderBy(x => x.Created).FirstOrDefault();
        var firstExpenditure = (await _expenditureRepository.GetExpendituresAsync(accountEmail))
            .OrderBy(x => x.Created).FirstOrDefault();

        DateTime firstCreated = DateTime.MaxValue;
        if (firstIncome != null && firstIncome.Created != DateTime.MinValue && firstIncome.Created < firstCreated)
            firstCreated = firstIncome.Created;
        if (firstExpenditure != null && firstExpenditure.Created != DateTime.MinValue && firstExpenditure.Created < firstCreated)
            firstCreated = firstExpenditure.Created;
        if (firstCreated == DateTime.MaxValue)
            firstCreated = DateTime.UtcNow;

        // Convert to user timezone
        try { firstCreated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(firstCreated, timeZoneId); } catch { /* keep UTC */ }

        summary.StartYear = firstCreated.Year;

        return summary;
    }

    /// <inheritdoc />
    public async Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit)
    {
        var assets = (await _assetRepository.GetAssetsAsync(accountEmail)) ?? [];
        var account = await _accountRepository.GetAccountByEmailAsync(accountEmail);
        if (account == null) return;

        account.DefaultMonetaryUnit = assets.Select(x => x.MonetaryUnit).Distinct().Contains(monetaryUnit)
            ? monetaryUnit
            : null;

        await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
    }

    /// <inheritdoc />
    public async Task<NotificationCounts> GetNoticeCountsAsync(string email, int noticeMaturityDateDay)
    {
        int incomesNoticed = 0, expendituresNoticed = 0;
        int incomesExpired = 0, expendituresExpired = 0;

        DateTime currentDate = DateTime.UtcNow.Date;

        foreach (var item in await _fixedIncomeRepository.GetFixedIncomesAsync(email))
        {
            bool noticed = false;
            try
            {
                var paymentDate = new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay);
                double daysUntilPayment = paymentDate.Subtract(currentDate).TotalDays;
                noticed = daysUntilPayment >= 0 && daysUntilPayment <= noticeMaturityDateDay;
            }
            catch { /* invalid DepositMonth/Day combination - treat as not noticed */ }
            if (item.Unpunctuality) noticed = true;
            if (noticed) incomesNoticed++;
            if (item.MaturityDate.Subtract(currentDate).TotalDays < 0) incomesExpired++;
        }

        foreach (var item in await _fixedExpenditureRepository.GetFixedExpendituresAsync(email))
        {
            bool noticed = false;
            try
            {
                var paymentDate = new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay);
                double daysUntilPayment = paymentDate.Subtract(currentDate).TotalDays;
                noticed = daysUntilPayment >= 0 && daysUntilPayment <= noticeMaturityDateDay;
            }
            catch { /* invalid DepositMonth/Day combination - treat as not noticed */ }
            if (item.Unpunctuality) noticed = true;
            if (noticed) expendituresNoticed++;
            if (item.MaturityDate.Subtract(currentDate).TotalDays < 0) expendituresExpired++;
        }

        return new NotificationCounts
        {
            FixedIncomesNoticed = incomesNoticed,
            FixedExpendituresNoticed = expendituresNoticed,
            FixedIncomesExpired = incomesExpired,
            FixedExpendituresExpired = expendituresExpired
        };
    }

    private async Task SyncDefaultMonetaryUnitInternalAsync(AccountResponse? account, List<AssetResponse> assets)
    {
        if (account == null) return;

        bool needsUpdate = false;
        if (account.DefaultMonetaryUnit == null && assets.Count == 0)
        {
            // nothing to do
        }
        else if (account.DefaultMonetaryUnit == null && assets.Count != 0)
        {
            var mostUsed = assets.GroupBy(a => a.MonetaryUnit).OrderByDescending(g => g.Count()).FirstOrDefault();
            account.DefaultMonetaryUnit = mostUsed?.Key;
            needsUpdate = true;
        }
        else if (account.DefaultMonetaryUnit != null && assets.Count == 0)
        {
            account.DefaultMonetaryUnit = null;
            needsUpdate = true;
        }
        else if (account.DefaultMonetaryUnit != null && assets.Count != 0)
        {
            if (!assets.Select(x => x.MonetaryUnit).Distinct().Contains(account.DefaultMonetaryUnit))
            {
                var mostUsed = assets.GroupBy(a => a.MonetaryUnit).OrderByDescending(g => g.Count()).FirstOrDefault();
                account.DefaultMonetaryUnit = mostUsed?.Key;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
    }

    /// <inheritdoc />
    public ServerResourceSummary GetServerResourceSummary(string hostResourceFilePath, string dockerResourceFilePath)
    {
        string hostText = File.Exists(hostResourceFilePath) ? File.ReadAllText(hostResourceFilePath) : string.Empty;
        string dockerText = File.Exists(dockerResourceFilePath) ? File.ReadAllText(dockerResourceFilePath) : string.Empty;

        string hostCpuInfo = ExtractSection(hostText, "Host CPU Information:", "Host Memory Information:");
        string hostMemoryInfo = ExtractSection(hostText, "Host Memory Information:", "Host Disk Information:");
        string hostDiskInfo = ExtractSection(hostText, "Host Disk Information:");
        string dockerResourceUsage = ExtractSection(dockerText, "Docker Resource Usage:");

        var dockerContainerResult = new List<string> { "Name||CPU %||MEM Usage / LIMIT" };
        try
        {
            var lines = dockerResourceUsage.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var resourceMap = new Dictionary<string, string[]>();
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                resourceMap[cols[0]] = cols;
            }
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string name = cols[0];
                if (resourceMap.TryGetValue(name, out var r))
                    dockerContainerResult.Add($"{name}||{r[1]}||{r[2]} {r[3]} {r[4]}");
            }
        }
        catch { }

        string memUsageLimit = string.Empty;
        if (!string.IsNullOrEmpty(hostMemoryInfo))
        {
            try
            {
                string[] temp = hostMemoryInfo.Replace("Memory: ", "").Split('/');
                if (temp.Length >= 2)
                    memUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch { }
        }

        string diskUsageLimit = string.Empty;
        if (!string.IsNullOrEmpty(hostDiskInfo))
        {
            try
            {
                string[] temp = hostDiskInfo.Replace("Disk: ", "").Split('/');
                if (temp.Length >= 2)
                    diskUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch { }
        }

        double.TryParse(
            new string(hostCpuInfo.Where(c => char.IsDigit(c) || c == '.').ToArray()),
            out double hostCpuNumeric);

        return new ServerResourceSummary
        {
            HostCpuNumeric = hostCpuNumeric,
            HostCpuInfo = hostCpuInfo,
            MemUsageLimit = memUsageLimit,
            HostMemoryInfo = hostMemoryInfo,
            DiskUsageLimit = diskUsageLimit,
            HostDiskInfo = hostDiskInfo,
            DockerContainerResult = dockerContainerResult
        };
    }

    private static string ExtractSection(string input, string startMarker, string? endMarker = null)
    {
        try
        {
            int startIndex = input.IndexOf(startMarker) + startMarker.Length;
            int endIndex = endMarker != null ? input.IndexOf(endMarker, startIndex) : input.Length;
            return input.Substring(startIndex, endIndex - startIndex).Trim();
        }
        catch { return string.Empty; }
    }
}
