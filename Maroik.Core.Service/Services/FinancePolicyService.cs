using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IFinancePolicyService"/>
public sealed class FinancePolicyService : IFinancePolicyService
{
    /// <inheritdoc />
    public bool TryParseAmount(string? input, out decimal amount) => FinanceAmountPolicy.TryParseInput(input, out amount);

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
}
