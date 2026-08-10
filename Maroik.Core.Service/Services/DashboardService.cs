using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Policies;
using Maroik.Core.Service.Mappers;

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
    public async Task<DashboardDto> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId, CancellationToken ct = default)
    {
        // Normalize year/month
        DateTime accountToday = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneId);
        if (!int.TryParse(year, out int yearInt)) yearInt = accountToday.Year;
        if (!int.TryParse(month, out int monthInt)) monthInt = accountToday.Month;

        List<Asset> assets = await _assetRepository.GetByAccountEmailAsync(accountEmail, ct);
        Account? account = await _accountRepository.FindByEmailAsync(accountEmail, ct);

        string? defaultMonetaryUnit = GetEffectiveMonetaryUnit(account, assets);

        var summary = new DashboardDto
        {
            DefaultMonetaryUnit = defaultMonetaryUnit,
            Assets = assets.Select(AssetMapper.ToResponse).ToList(),
            SelectedYear = yearInt,
            SelectedMonth = monthInt,
            EndYear = accountToday.Year,
            MonetaryUnits = assets.Select(x => x.Balance.Currency).Distinct()
        };

        if (summary.DefaultMonetaryUnit != null)
        {
            DateTime yearStart, yearEnd, ymStart, ymEnd;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                yearStart = TimeZoneInfo.ConvertTimeToUtc(new DateTime(yearInt, 1, 1), tz);
                yearEnd = TimeZoneInfo.ConvertTimeToUtc(new DateTime(yearInt + 1, 1, 1), tz);
                ymStart = TimeZoneInfo.ConvertTimeToUtc(new DateTime(yearInt, monthInt, 1), tz);
                ymEnd = TimeZoneInfo.ConvertTimeToUtc(new DateTime(yearInt, monthInt, 1).AddMonths(1), tz);
            }
            catch
            {
                yearStart = new DateTime(yearInt, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                yearEnd = new DateTime(yearInt + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                ymStart = new DateTime(yearInt, monthInt, 1, 0, 0, 0, DateTimeKind.Utc);
                ymEnd = ymStart.AddMonths(1);
            }

            List<Income> yearIncomes = await _incomeRepository.GetByAccountEmailAndDateRangeAsync(accountEmail, yearStart, yearEnd, ct);
            List<Expenditure> yearExpenditures = await _expenditureRepository.GetByAccountEmailAndDateRangeAsync(accountEmail, yearStart, yearEnd, ct);
            List<Income> ymIncomes = await _incomeRepository.GetByAccountEmailAndDateRangeAsync(accountEmail, ymStart, ymEnd, ct);
            List<Expenditure> ymExpenditures = await _expenditureRepository.GetByAccountEmailAndDateRangeAsync(accountEmail, ymStart, ymEnd, ct);

            // Filter by default monetary unit via asset lookup
            string defaultUnit = summary.DefaultMonetaryUnit;
            summary.YearIncomes = yearIncomes
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.Balance.Currency == defaultUnit)
                .Select(IncomeMapper.ToResponse).ToList();
            summary.YearExpenditures = yearExpenditures
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.Balance.Currency == defaultUnit)
                .Select(ExpenditureMapper.ToResponse).ToList();
            summary.YearMonthIncomes = ymIncomes
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.Balance.Currency == defaultUnit)
                .Select(IncomeMapper.ToResponse).ToList();
            summary.YearMonthExpenditures = ymExpenditures
                .Where(x => assets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.Balance.Currency == defaultUnit)
                .Select(ExpenditureMapper.ToResponse).ToList();
        }

        // Compute StartYear
        var firstIncome = await _incomeRepository.GetFirstByAccountEmailOrderedByCreatedAsync(accountEmail, ct);
        var firstExpenditure = await _expenditureRepository.GetFirstByAccountEmailOrderedByCreatedAsync(accountEmail, ct);

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
    public async Task<NotificationDto> GetNoticeCountsAsync(string email, int noticeMaturityDateDay, string timeZoneId, CancellationToken ct = default)
    {
        int incomesNoticed = 0, expendituresNoticed = 0;
        int incomesExpired = 0, expendituresExpired = 0;

        DateTime currentDate = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneId).Date;

        foreach (var item in await _fixedIncomeRepository.GetByAccountEmailAsync(email, ct))
        {
            if (FixedSchedulePolicy.IsNoticed(item.DepositMonth, item.DepositDay, currentDate, noticeMaturityDateDay, item.Unpunctuality))
                incomesNoticed++;
            if (FixedSchedulePolicy.IsExpired(item.MaturityDate, currentDate))
                incomesExpired++;
        }

        foreach (var item in await _fixedExpenditureRepository.GetByAccountEmailAsync(email, ct))
        {
            if (FixedSchedulePolicy.IsNoticed(item.DepositMonth, item.DepositDay, currentDate, noticeMaturityDateDay, item.Unpunctuality))
                expendituresNoticed++;
            if (FixedSchedulePolicy.IsExpired(item.MaturityDate, currentDate))
                expendituresExpired++;
        }

        return new NotificationDto
        {
            FixedIncomesNoticed = incomesNoticed,
            FixedExpendituresNoticed = expendituresNoticed,
            FixedIncomesExpired = incomesExpired,
            FixedExpendituresExpired = expendituresExpired
        };
    }

    /// <summary>
    /// Computes the monetary unit the dashboard should display without persisting any changes.
    /// When the stored unit is missing or no longer valid (asset deleted, no assets), returns
    /// the best available unit so the view remains consistent until the user explicitly saves.
    /// </summary>
    private static string? GetEffectiveMonetaryUnit(Account? account, List<Asset> assets)
    {
        if (account == null) return null;

        string? currentUnit = account.DefaultMonetaryUnit;
        HashSet<string> availableUnits = assets.Select(a => a.Balance.Currency).ToHashSet();

        if (availableUnits.Count == 0) return null;

        if (currentUnit != null && availableUnits.Contains(currentUnit)) return currentUnit;

        return assets.GroupBy(a => a.Balance.Currency).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
    }

    /// <inheritdoc />
    public ServerResourceDto GetServerResourceSummary(string hostResourceFilePath, string dockerResourceFilePath)
    {
        string hostText = File.Exists(hostResourceFilePath) ? File.ReadAllText(hostResourceFilePath) : "";
        string dockerText = File.Exists(dockerResourceFilePath) ? File.ReadAllText(dockerResourceFilePath) : "";

        string hostCpuInfo = ExtractSection(hostText, "Host CPU Information:", "Host Memory Information:");
        string hostMemoryInfo = ExtractSection(hostText, "Host Memory Information:", "Host Disk Information:");
        string hostDiskInfo = ExtractSection(hostText, "Host Disk Information:");
        string dockerResourceUsage = ExtractSection(dockerText, "Docker Resource Usage:");

        var dockerContainerResult = new List<DockerContainerResourceDto>();
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
                {
                    dockerContainerResult.Add(new DockerContainerResourceDto
                    {
                        Name = name,
                        CpuPercent = r[1],
                        MemUsageDisplay = $"{r[2]} {r[3]} {r[4]}"
                    });
                }
            }
        }
        catch
        {
            // ignored
        }

        string memUsageLimit = "";
        if (!string.IsNullOrEmpty(hostMemoryInfo))
        {
            try
            {
                string[] temp = hostMemoryInfo.Replace("Memory: ", "").Split('/');
                if (temp.Length >= 2)
                    memUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch
            {
                // ignored
            }
        }

        string diskUsageLimit = "";
        if (!string.IsNullOrEmpty(hostDiskInfo))
        {
            try
            {
                string[] temp = hostDiskInfo.Replace("Disk: ", "").Split('/');
                if (temp.Length >= 2)
                    diskUsageLimit = temp[1].Trim() + " / " + temp[0].Trim();
            }
            catch
            {
                // ignored
            }
        }

#pragma warning disable CA1806
        double.TryParse(
#pragma warning restore CA1806
            new string(hostCpuInfo.Where(c => char.IsDigit(c) || c == '.').ToArray()),
            out double hostCpuNumeric);

        return new ServerResourceDto
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

    /// <summary>
    /// Extracts and trims the text between <paramref name="startMarker"/> and <paramref name="endMarker"/>
    /// (or to the end of <paramref name="input"/> when <paramref name="endMarker"/> is omitted).
    /// Returns an empty string when a marker is missing or any parsing error occurs, instead of throwing,
    /// so a malformed resource log never breaks the whole summary.
    /// </summary>
    private static string ExtractSection(string input, string startMarker, string? endMarker = null)
    {
        try
        {
            int startIndex = input.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
            int endIndex = endMarker != null ? input.IndexOf(endMarker, startIndex, StringComparison.Ordinal) : input.Length;
            return input.Substring(startIndex, endIndex - startIndex).Trim();
        }
        catch { return ""; }
    }
}
