using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The finance rules the Website needs; each member answers from a Domain policy.</summary>
public interface IFinancePolicyService
{
    /// <summary>Reads the text an amount field posted; false when it is not an amount.</summary>
    bool TryParseAmount(string? input, out decimal amount);

    /// <summary>Whether an expenditure subclass is a transfer, which needs a deposit asset.</summary>
    bool IsDepositAssetSubClass(string? subClass);

    /// <summary>Whether a fixed schedule's next deposit is inside the notice window.</summary>
    bool IsFixedScheduleNoticed(int depositMonth, int depositDay, DateTime today, int noticeWindowDays, bool unpunctuality);

    /// <summary>Whether a fixed schedule's maturity date has passed.</summary>
    bool IsFixedScheduleExpired(DateTime maturityDate, DateTime today);

    /// <summary>How a fixed schedule's row stands, from its two flags.</summary>
    FixedScheduleRowState GetFixedScheduleRowState(bool expired, bool noticed);

    /// <summary>What the finance forms offer and mirror.</summary>
    FinanceFormRules GetFinanceFormRules();
}
