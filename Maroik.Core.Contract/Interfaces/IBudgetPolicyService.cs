using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>What the budget page needs from the Domain.</summary>
public interface IBudgetPolicyService
{
    /// <summary>What the budget page needs to know about one sheet kind.</summary>
    BudgetPageRules GetBudgetPageRules(BudgetSheetKind kind);
}
