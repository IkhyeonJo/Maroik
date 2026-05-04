using ErrorOr;

namespace Maroik.Core.Contract.Domains.Finance;

/// <summary>
/// Domain policy that validates and classifies expenditure main/sub-class combinations.
/// Returns <c>true</c> when the combination requires a deposit asset (transfer-type spending),
/// <c>false</c> when no deposit asset is involved, or an error when the combination is invalid.
/// </summary>
public static class ExpenditureClassPolicy
{
    /// <summary>
    /// Validates <paramref name="mainClass"/> / <paramref name="subClass"/> and returns whether
    /// the expenditure requires a deposit asset (<c>true</c>) or not (<c>false</c>).
    /// Returns a validation error if the combination is not recognised.
    /// </summary>
    public static ErrorOr<bool> Validate(string? mainClass, string? subClass) => mainClass switch
    {
        "RegularSavings" => subClass is "Deposit" or "Investment"
            ? (ErrorOr<bool>)true
            : Error.Validation("ExpenditureClass.Invalid", "Invalid sub-class for RegularSavings."),

        "NonConsumerSpending" => subClass switch
        {
            "PublicPension" or "DebtRepayment" => (ErrorOr<bool>)true,
            "Tax" or "SocialInsurance" or "InterHouseholdTranserExpenses"
                or "NonProfitOrganizationTransfer" => (ErrorOr<bool>)false,
            _ => Error.Validation("ExpenditureClass.Invalid", "Invalid sub-class for NonConsumerSpending.")
        },

        "ConsumerSpending" => subClass is
            "MealOrEatOutExpenses" or "HousingOrSuppliesCost" or "EducationExpenses" or
            "MedicalExpenses" or "TransportationCost" or "CommunicationCost" or
            "LeisureOrCulture" or "ClothingOrShoes" or "PinMoney" or
            "ProtectionTypeInsurance" or "OtherExpenses" or "UnknownExpenditure"
                ? (ErrorOr<bool>)false
                : Error.Validation("ExpenditureClass.Invalid", "Invalid sub-class for ConsumerSpending."),

        _ => Error.Validation("ExpenditureClass.Invalid", "Unknown expenditure main class.")
    };
}
