using ErrorOr;
using Maroik.Core.Domain.Localization;

namespace Maroik.Core.Domain.Finance;

/// <summary>
/// The <c>Amount</c> validation rule shared by every Finance aggregate (<see cref="Income"/>,
/// <see cref="Expenditure"/>, <see cref="FixedIncome"/>, <see cref="FixedExpenditure"/>).
/// Sibling to <see cref="FinanceTextPolicy"/>.
/// </summary>
public static class FinanceAmountPolicy
{
    /// <summary>
    /// Returns a validation error when <paramref name="amount"/> is negative; otherwise
    /// <see cref="Result.Success"/>.
    /// </summary>
    public static ErrorOr<Success> ValidateNonNegative(decimal amount)
    {
        if (amount < 0m)
            return LocalizableError.Validation("Finance.NegativeAmount", "Amount cannot be negative.");

        return Result.Success;
    }
}
