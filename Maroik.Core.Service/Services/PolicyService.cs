using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.Formulas;
using Maroik.Core.Domain.Uploads;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IPolicyService"/>
public sealed class PolicyService : IPolicyService
{
    /// <summary>The month columns of a budget sheet, in column order.</summary>
    private static readonly IReadOnlyDictionary<string, int> _monthByColumn =
        Enumerable.Range(0, BudgetSheetLayout.ColumnCount)
            .Select(i => ((char)('A' + i)).ToString())
            .Select(column => (column, month: BudgetSheetLayout.MonthOf(column)))
            .Where(c => c.month is not null)
            .ToDictionary(c => c.column, c => c.month!.Value, StringComparer.Ordinal);

    /// <summary>The functions the formula editor offers.</summary>
    private static readonly IReadOnlyList<BudgetFunctionResponse> _budgetFunctions =
        [.. FormulaFunctions.All.Select(f => new BudgetFunctionResponse(f.Name, f.MinArguments, f.MaxArguments, f.Signature))];

    /// <inheritdoc />
    public bool TryParseAmount(string? input, out decimal amount) => FinanceAmountPolicy.TryParseInput(input, out amount);

    /// <inheritdoc />
    public bool IsAllowedImageExtension(string? extension) => ImageUploadPolicy.IsAllowedExtension(extension);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllowedImageContentTypes() => ImageUploadPolicy.AllowedContentTypes;

    /// <inheritdoc />
    public (ServiceResult Result, long Bytes) FileManagerQuotaBytesFromGb(decimal gb)
    {
        var quota = FileManagerUploadPolicy.QuotaBytesFromGb(gb);
        return quota.IsError ? (ServiceResult.FromError(quota.FirstError), 0) : (ServiceResult.Ok(), quota.Value);
    }

    /// <inheritdoc />
    public decimal FileManagerQuotaGbFromBytes(long bytes) => FileManagerUploadPolicy.QuotaGbFromBytes(bytes);

    /// <inheritdoc />
    public bool IsPasswordWithinMaxByteLength(string? password) => PasswordPolicy.IsWithinMaxByteLength(password);

    /// <inheritdoc />
    public bool IsWithinCalendarEventYears(DateTime date) => CalendarEvent.IsWithinYears(date);

    /// <inheritdoc />
    public bool IsDepositAssetSubClass(string? subClass) =>
        subClass != null && ExpenditureClassPolicy.DepositAssetSubClasses.Contains(subClass);

    /// <inheritdoc />
    public bool IsFixedScheduleNoticed(int depositMonth, int depositDay, DateTime today, int noticeWindowDays, bool unpunctuality) =>
        FixedSchedulePolicy.IsNoticed(depositMonth, depositDay, today, noticeWindowDays, unpunctuality);

    /// <inheritdoc />
    public bool IsFixedScheduleExpired(DateTime maturityDate, DateTime today) => FixedSchedulePolicy.IsExpired(maturityDate, today);

    /// <inheritdoc />
    public FixedScheduleRowState GetFixedScheduleRowState(bool expired, bool noticed) => FixedSchedulePolicy.GetRowStatus(expired, noticed) switch
    {
        FixedScheduleRowStatus.Expired => FixedScheduleRowState.Expired,
        FixedScheduleRowStatus.Noticed => FixedScheduleRowState.Noticed,
        _ => FixedScheduleRowState.Normal,
    };

    /// <inheritdoc />
    public string? GetSessionInvalidationReason(AccountResponse? current, AccountResponse session) =>
        AccountSessionPolicy.InvalidationReason(current != null, current?.Deleted ?? false, current?.SecurityStamp, session.SecurityStamp)?.ToString();

    /// <inheritdoc />
    public string GetAccountCreatedMessage() => AccountMessage.Success.GetDescription();

    /// <inheritdoc />
    public FinanceFormRules GetFinanceFormRules() => new()
    {
        IncomeMainClasses = IncomeClassPolicy.MainClasses,
        IncomeSubClassesByMainClass = IncomeClassPolicy.SubClassesByMainClass,
        ExpenditureMainClasses = ExpenditureClassPolicy.MainClasses,
        ExpenditureSubClassesByMainClass = ExpenditureClassPolicy.SubClassesByMainClass,
        DepositAssetSubClasses = ExpenditureClassPolicy.DepositAssetSubClasses,
        MaxDepositDayByMonth = FixedSchedulePolicy.MaxDepositDayByMonth,
        NoMaturityDateIso = FixedSchedulePolicy.NoMaturityDateIso,
        AssetItemTypes = Enum.GetNames<AssetItemType>(),
    };

    /// <inheritdoc />
    public CalendarFormRules GetCalendarFormRules() => new()
    {
        MinLeadTimeBeforeEvent = CalendarReminderPolicy.MinLeadTimeBeforeEvent,
        MaxMinutesBeforeEvent = CalendarReminderPolicy.MaxMinutesBeforeEvent,
        MaxHoursBeforeEvent = CalendarReminderPolicy.MaxHoursBeforeEvent,
        MaxDaysBeforeEvent = CalendarReminderPolicy.MaxDaysBeforeEvent,
        MaxWeeksBeforeEvent = CalendarReminderPolicy.MaxWeeksBeforeEvent,
        ReminderTimeOfDayStepMinutes = CalendarReminderPolicy.ReminderTimeOfDayStepMinutes,
        DefaultReminderTimeOfDay = CalendarReminderPolicy.DefaultReminderTimeOfDay,
        MaxRemindersPerEvent = CalendarReminderPolicy.MaxRemindersPerEvent,
    };

    /// <inheritdoc />
    public FileManagerFormRules GetFileManagerFormRules() => new()
    {
        ScanPollMilliseconds = FileManagerUploadPolicy.ScanPollMilliseconds,
        MaxScanWaitMilliseconds = FileManagerUploadPolicy.MaxScanWaitMilliseconds,
    };

    /// <inheritdoc />
    public BudgetPageRules GetBudgetPageRules(BudgetSheetKind kind)
    {
        BudgetKind domainKind = BudgetMapper.ToDomain(kind);
        return new BudgetPageRules
        {
            MainClasses = BudgetLine.MainClasses(domainKind),
            SubClassesByMainClass = BudgetLine.Taxonomy(domainKind),
            MonthByColumn = _monthByColumn,
            YearColumn = BudgetSheetLayout.YearColumn,
            Functions = _budgetFunctions,
        };
    }
}
