using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.Formulas;
using Maroik.Core.Domain.Uploads;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="PolicyService"/>: each member answers what the Domain's policy answers.</summary>
public class PolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly PolicyService _sut = new();

    /// <summary>An amount field's text is read, and anything else refused.</summary>
    [Theory]
    [InlineData("1,234.5", true, 1234.5)]
    [InlineData("12x", false, 0)]
    [InlineData(null, false, 0)]
    public void TryParseAmount_ReadsWhatThePolicyReads(string? input, bool parsed, double expected)
    {
        Assert.Equal(parsed, _sut.TryParseAmount(input, out decimal amount));
        Assert.Equal((decimal)expected, amount);
    }

    /// <summary>Only the policy's image extensions are allowed.</summary>
    [Theory]
    [InlineData(".png", true)]
    [InlineData(".gif", false)]
    [InlineData(null, false)]
    public void IsAllowedImageExtension_FollowsThePolicy(string? extension, bool expected) =>
        Assert.Equal(expected, _sut.IsAllowedImageExtension(extension));

    /// <summary>The allowed content types are the policy's.</summary>
    [Fact]
    public void GetAllowedImageContentTypes_AreThePolicys() =>
        Assert.Equal(ImageUploadPolicy.AllowedContentTypes, _sut.GetAllowedImageContentTypes());

    /// <summary>A valid quota comes back in bytes with a successful result.</summary>
    [Fact]
    public void FileManagerQuotaBytesFromGb_TurnsGigabytesIntoBytes()
    {
        (ServiceResult result, long bytes) = _sut.FileManagerQuotaBytesFromGb(2m);

        Assert.True(result.Success);
        Assert.Equal(FileManagerUploadPolicy.QuotaBytesFromGb(2m).Value, bytes);
    }

    /// <summary>A negative quota comes back as the policy's refusal, with its message key.</summary>
    [Fact]
    public void FileManagerQuotaBytesFromGb_RefusesANegativeQuota()
    {
        (ServiceResult result, long bytes) = _sut.FileManagerQuotaBytesFromGb(-1m);

        Assert.False(result.Success);
        Assert.Equal(ServiceResult.FromError(FileManagerUploadPolicy.QuotaBytesFromGb(-1m).FirstError).ErrorKey, result.ErrorKey);
        Assert.Equal(0, bytes);
    }

    /// <summary>Bytes are turned back into the admin form's gigabytes.</summary>
    [Fact]
    public void FileManagerQuotaGbFromBytes_TurnsBytesIntoGigabytes() =>
        Assert.Equal(FileManagerUploadPolicy.QuotaGbFromBytes(1_610_612_736), _sut.FileManagerQuotaGbFromBytes(1_610_612_736));

    /// <summary>The byte limit counts bytes, not characters.</summary>
    [Fact]
    public void IsPasswordWithinMaxByteLength_CountsBytes()
    {
        Assert.True(_sut.IsPasswordWithinMaxByteLength(new string('a', 72)));
        Assert.False(_sut.IsPasswordWithinMaxByteLength(new string('가', 25)));
    }

    /// <summary>The event years stop before DateTime's edges.</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(9999, false)]
    public void IsWithinCalendarEventYears_FollowsTheAggregate(int year, bool expected) =>
        Assert.Equal(expected, _sut.IsWithinCalendarEventYears(new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)));

    /// <summary>A transfer subclass needs a deposit asset; others do not.</summary>
    [Fact]
    public void IsDepositAssetSubClass_FollowsThePolicy()
    {
        Assert.True(_sut.IsDepositAssetSubClass(nameof(ExpenditureSubClass.Deposit)));
        Assert.False(_sut.IsDepositAssetSubClass(nameof(ExpenditureSubClass.Tax)));
        Assert.False(_sut.IsDepositAssetSubClass(null));
    }

    /// <summary>The notice window and the unpunctuality flag both notice a schedule.</summary>
    [Fact]
    public void IsFixedScheduleNoticed_FollowsThePolicy()
    {
        DateTime today = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.True(_sut.IsFixedScheduleNoticed(6, 3, today, 7, unpunctuality: false));
        Assert.False(_sut.IsFixedScheduleNoticed(9, 3, today, 7, unpunctuality: false));
        Assert.True(_sut.IsFixedScheduleNoticed(9, 3, today, 7, unpunctuality: true));
    }

    /// <summary>A schedule has expired once its maturity date is behind today.</summary>
    [Fact]
    public void IsFixedScheduleExpired_FollowsThePolicy()
    {
        DateTime today = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.True(_sut.IsFixedScheduleExpired(today.AddDays(-1), today));
        Assert.False(_sut.IsFixedScheduleExpired(today.AddDays(1), today));
    }

    /// <summary>The state is the policy's status, with expired winning over noticed.</summary>
    [Theory]
    [InlineData(true, true, FixedScheduleRowState.Expired)]
    [InlineData(false, true, FixedScheduleRowState.Noticed)]
    [InlineData(false, false, FixedScheduleRowState.Normal)]
    public void GetFixedScheduleRowState_FollowsThePolicy(bool expired, bool noticed, FixedScheduleRowState expected) =>
        Assert.Equal(expected, _sut.GetFixedScheduleRowState(expired, noticed));

    /// <summary>A session holds while the account exists, is not deleted and keeps its stamp.</summary>
    [Fact]
    public void GetSessionInvalidationReason_NamesWhyASessionEnded()
    {
        AccountResponse session = new() { SecurityStamp = "a" };

        Assert.Null(_sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "a" }, session));
        Assert.Equal("AccountMissing", _sut.GetSessionInvalidationReason(null, session));
        Assert.Equal("AccountDeleted", _sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "a", Deleted = true }, session));
        Assert.Equal("SecurityStampChanged", _sut.GetSessionInvalidationReason(new AccountResponse { SecurityStamp = "b" }, session));
    }

    /// <summary>A new account's message is the domain's "Success".</summary>
    [Fact]
    public void GetAccountCreatedMessage_IsSuccess() => Assert.Equal("Success", _sut.GetAccountCreatedMessage());

    /// <summary>The finance rules carry the policies' taxonomies and schedule values.</summary>
    [Fact]
    public void GetFinanceFormRules_CarriesThePolicies()
    {
        FinanceFormRules rules = _sut.GetFinanceFormRules();

        Assert.Equal(IncomeClassPolicy.MainClasses, rules.IncomeMainClasses);
        Assert.Equal(IncomeClassPolicy.SubClassesByMainClass, rules.IncomeSubClassesByMainClass);
        Assert.Equal(ExpenditureClassPolicy.MainClasses, rules.ExpenditureMainClasses);
        Assert.Equal(ExpenditureClassPolicy.SubClassesByMainClass, rules.ExpenditureSubClassesByMainClass);
        Assert.Equal(ExpenditureClassPolicy.DepositAssetSubClasses, rules.DepositAssetSubClasses);
        Assert.Equal(FixedSchedulePolicy.MaxDepositDayByMonth, rules.MaxDepositDayByMonth);
        Assert.Equal(FixedSchedulePolicy.NoMaturityDateIso, rules.NoMaturityDateIso);
        Assert.Equal(Enum.GetNames<AssetItemType>(), rules.AssetItemTypes);
    }

    /// <summary>The calendar rules carry the reminder policy's limits.</summary>
    [Fact]
    public void GetCalendarFormRules_CarriesThePolicy()
    {
        CalendarFormRules rules = _sut.GetCalendarFormRules();

        Assert.Equal(CalendarReminderPolicy.MinLeadTimeBeforeEvent, rules.MinLeadTimeBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxMinutesBeforeEvent, rules.MaxMinutesBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxHoursBeforeEvent, rules.MaxHoursBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxDaysBeforeEvent, rules.MaxDaysBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxWeeksBeforeEvent, rules.MaxWeeksBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.ReminderTimeOfDayStepMinutes, rules.ReminderTimeOfDayStepMinutes);
        Assert.Equal(CalendarReminderPolicy.DefaultReminderTimeOfDay, rules.DefaultReminderTimeOfDay);
        Assert.Equal(CalendarReminderPolicy.MaxRemindersPerEvent, rules.MaxRemindersPerEvent);
    }

    /// <summary>The File Manager rules carry the upload policy's timings.</summary>
    [Fact]
    public void GetFileManagerFormRules_CarriesThePolicy()
    {
        FileManagerFormRules rules = _sut.GetFileManagerFormRules();

        Assert.Equal(FileManagerUploadPolicy.ScanPollMilliseconds, rules.ScanPollMilliseconds);
        Assert.Equal(FileManagerUploadPolicy.MaxScanWaitMilliseconds, rules.MaxScanWaitMilliseconds);
    }

    /// <summary>Each sheet kind gets its own taxonomy, and the columns follow the layout.</summary>
    [Theory]
    [InlineData(BudgetSheetKind.Income, BudgetKind.Income)]
    [InlineData(BudgetSheetKind.Expenditure, BudgetKind.Expenditure)]
    public void GetBudgetPageRules_CarriesTheKindsTaxonomyAndTheLayout(BudgetSheetKind kind, BudgetKind domainKind)
    {
        BudgetPageRules rules = _sut.GetBudgetPageRules(kind);

        Assert.Equal(BudgetLine.MainClasses(domainKind), rules.MainClasses);
        Assert.Equal(BudgetLine.Taxonomy(domainKind), rules.SubClassesByMainClass);
        Assert.Equal(BudgetSheetLayout.YearColumn, rules.YearColumn);
        Assert.Equal(12, rules.MonthByColumn.Count);
        Assert.Equal(1, rules.MonthByColumn["B"]);
        Assert.Equal(12, rules.MonthByColumn["M"]);
        Assert.Equal(
            FormulaFunctions.All.Select(f => (f.Name, f.MinArguments, f.MaxArguments, f.Signature)),
            rules.Functions.Select(f => (f.Name, f.MinArguments, f.MaxArguments, f.Signature)));
    }
}
