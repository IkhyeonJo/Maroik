using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DashboardService> _logger;

    /// <summary>Initializes a new instance of <see cref="DashboardService"/> with the supplied dependencies.</summary>
    public DashboardService(
        IAccountRepository accountRepository,
        IAssetRepository assetRepository,
        IIncomeRepository incomeRepository,
        IExpenditureRepository expenditureRepository,
        IFixedIncomeRepository fixedIncomeRepository,
        IFixedExpenditureRepository fixedExpenditureRepository,
        ILogger<DashboardService> logger)
    {
        _accountRepository = accountRepository;
        _assetRepository = assetRepository;
        _incomeRepository = incomeRepository;
        _expenditureRepository = expenditureRepository;
        _fixedIncomeRepository = fixedIncomeRepository;
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DashboardDto> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId, CancellationToken ct = default)
    {
        // Normalize year/month
        DateTime accountToday = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneId);
        if (!int.TryParse(year, out int yearInt)) yearInt = accountToday.Year;
        if (!int.TryParse(month, out int monthInt)) monthInt = accountToday.Month;

        // An out-of-range month (or year outside DateTime's valid range) must be caught here rather
        // than left to throw from `new DateTime(yearInt, monthInt, 1)` below — the catch block there
        // only guards against an invalid timeZoneId and reconstructs the same DateTime with the same
        // values, so an invalid month would throw a second time, unhandled.
        if (monthInt is < 1 or > 12) monthInt = accountToday.Month;
        // Capped one below DateTime.MaxValue's year: yearEnd below computes `new DateTime(yearInt + 1, ...)`.
        if (yearInt is < 1 or > 9998) yearInt = accountToday.Year;

        // main computes DefaultMonetaryUnit/MonetaryUnits from the account's full asset list,
        // including deleted ones (AssetRepository.GetAssetsAsync never filters Deleted) — so a
        // user whose only assets happen to be deleted still resolves the same effective currency
        // and sees the same MonetaryUnits options main would show. Only the visible asset card
        // list (`assets` below) is deleted-filtered, since main has no such list to match.
        List<Asset> allAssets = await _assetRepository.GetByAccountEmailAsync(accountEmail, ct);
        List<Asset> assets = [.. allAssets.Where(a => !a.Deleted)];
        Account? account = await _accountRepository.FindByEmailAsync(accountEmail, ct);

        string? defaultMonetaryUnit = GetEffectiveMonetaryUnit(account, allAssets);

        // Persist the self-corrected value (matches main): other screens that read
        // Account.DefaultMonetaryUnit directly (e.g. account management) must not see a
        // stale/empty value while the dashboard itself already shows the resolved one.
        if (account != null && account.DefaultMonetaryUnit != defaultMonetaryUnit)
        {
            account.SetDefaultMonetaryUnit(defaultMonetaryUnit);
            await _accountRepository.UpdateEntityAsync(account, ct);
        }

        var summary = new DashboardDto
        {
            DefaultMonetaryUnit = defaultMonetaryUnit,
            Assets = assets.Select(AssetMapper.ToResponse).ToList(),
            SelectedYear = yearInt,
            SelectedMonth = monthInt,
            EndYear = accountToday.Year,
            MonetaryUnits = allAssets.Select(x => x.Balance.Currency).Distinct()
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

            // ymStart/ymEnd always fall within [yearStart, yearEnd), so the year-month totals can
            // be derived from the year totals already fetched above instead of two more DB round
            // trips (repositories share a single scoped DbContext, so these calls cannot run
            // concurrently via Task.WhenAll either).
            List<Income> ymIncomes = [.. yearIncomes.Where(x => x.Created >= ymStart && x.Created < ymEnd)];
            List<Expenditure> ymExpenditures = [.. yearExpenditures.Where(x => x.Created >= ymStart && x.Created < ymEnd)];

            // Filter by default monetary unit via asset lookup. Uses allAssets (not the
            // deleted-filtered `assets`) so a transaction tied to a since-deleted asset still
            // resolves its currency correctly instead of being dropped from the totals.
            string defaultUnit = summary.DefaultMonetaryUnit;
            summary.YearIncomes = yearIncomes
                .Where(x => allAssets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.Balance.Currency == defaultUnit)
                .Select(IncomeMapper.ToResponse).ToList();
            summary.YearExpenditures = yearExpenditures
                .Where(x => allAssets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.Balance.Currency == defaultUnit)
                .Select(ExpenditureMapper.ToResponse).ToList();
            summary.YearMonthIncomes = ymIncomes
                .Where(x => allAssets.FirstOrDefault(a => a.ProductName == x.DepositMyAssetProductName)?.Balance.Currency == defaultUnit)
                .Select(IncomeMapper.ToResponse).ToList();
            summary.YearMonthExpenditures = ymExpenditures
                .Where(x => allAssets.FirstOrDefault(a => a.ProductName == x.PaymentMethod)?.Balance.Currency == defaultUnit)
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
    /// When the stored unit is missing or no longer valid (no matching asset, no assets), returns
    /// the best available unit so the view remains consistent until the user explicitly saves.
    /// <paramref name="allAssets"/> must be the account's full asset list (deleted included), to
    /// match main's <c>AssetRepository.GetAssetsAsync</c>, which never filters on Deleted.
    /// </summary>
    private static string? GetEffectiveMonetaryUnit(Account? account, List<Asset> allAssets)
    {
        if (account == null) return null;

        string? currentUnit = account.DefaultMonetaryUnit;
        HashSet<string> availableUnits = allAssets.Select(a => a.Balance.Currency).ToHashSet();

        if (availableUnits.Count == 0) return null;

        if (currentUnit != null && availableUnits.Contains(currentUnit)) return currentUnit;

        return allAssets.GroupBy(a => a.Balance.Currency).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
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
                if (cols.Length < 5) continue;
                resourceMap[cols[0]] = cols;
            }
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 5) continue;
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Docker container resource usage");
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
            int markerIndex = input.IndexOf(startMarker, StringComparison.Ordinal);
            if (markerIndex < 0) return "";

            int startIndex = markerIndex + startMarker.Length;
            int endIndex = endMarker != null ? input.IndexOf(endMarker, startIndex, StringComparison.Ordinal) : input.Length;
            return endIndex < 0 ? "" : input.Substring(startIndex, endIndex - startIndex).Trim();
        }
        catch { return ""; }
    }
}
