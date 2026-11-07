using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="FinancePolicyService"/>: each member answers what the Domain answers.</summary>
public class FinancePolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly FinancePolicyService _sut = new();

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
}
