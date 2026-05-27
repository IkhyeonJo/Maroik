using ErrorOr;

namespace Maroik.Core.Contract.Domains.Finance;

/// <summary>
/// Domain policy that validates income main/sub-class combinations.
/// Returns <see cref="Result.Success"/> when the combination is recognised,
/// or a validation error when it is not.
/// </summary>
public static class IncomeClassPolicy
{
    /// <summary>
    /// Validates <paramref name="mainClass"/> / <paramref name="subClass"/>.
    /// Returns a validation error if the combination is not recognised.
    /// </summary>
    public static ErrorOr<Success> Validate(string? mainClass, string? subClass) => mainClass switch
    {
        "RegularIncome" => subClass is "LaborIncome" or "BusinessIncome" or "PensionIncome"
            or "FinancialIncome" or "RentalIncome" or "OtherIncome"
                ? Result.Success
                : Error.Validation("IncomeClass.Invalid", "Invalid sub-class for RegularIncome."),

        "IrregularIncome" => subClass is "LaborIncome" or "OtherIncome"
            ? Result.Success
            : Error.Validation("IncomeClass.Invalid", "Invalid sub-class for IrregularIncome."),

        _ => Error.Validation("IncomeClass.Invalid", "Unknown income main class.")
    };
}
