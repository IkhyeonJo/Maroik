using ErrorOr;
using Maroik.Core.Domain.Localization;

namespace Maroik.Core.Domain.Finance;

/// <summary>
/// Domain policy that validates and classifies expenditure main/subclass combinations.
/// Returns <c>true</c> when the combination requires a deposit asset (transfer-type spending),
/// <c>false</c> when no deposit asset is involved, or an error when the combination is invalid.
/// <para>
/// <see cref="SubClassesByMainClass"/> and <see cref="DepositAssetSubClasses"/> are the single
/// source of truth; <see cref="Validate"/> is derived from them, and the presentation layer
/// serializes them into the create/edit forms so the client no longer hard-codes the same lists.
/// </para>
/// </summary>
public static class ExpenditureClassPolicy
{
    /// <summary>
    /// The recognized sub-classes for each expenditure main class, in display order.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SubClassesByMainClass =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["RegularSavings"] = new[] { "Deposit", "Investment" },
            ["NonConsumerSpending"] = new[]
            {
                "PublicPension", "DebtRepayment", "Tax", "SocialInsurance",
                "InterHouseholdTransferExpenses", "NonProfitOrganizationTransfer"
            },
            ["ConsumerSpending"] = new[]
            {
                "MealOrEatOutExpenses", "HousingOrSuppliesCost", "EducationExpenses",
                "MedicalExpenses", "TransportationCost", "CommunicationCost",
                "LeisureOrCulture", "ClothingOrShoes", "PinMoney",
                "ProtectionTypeInsurance", "OtherExpenses", "UnknownExpenditure"
            }
        };

    /// <summary>
    /// Sub-classes that represent a transfer into an asset and therefore require the user to
    /// pick a deposit asset. Every other recognized sub-class does not.
    /// </summary>
    public static readonly IReadOnlyList<string> DepositAssetSubClasses =
        new[] { "Deposit", "Investment", "PublicPension", "DebtRepayment" };

    /// <summary>
    /// Validates <paramref name="mainClass"/> / <paramref name="subClass"/> and returns whether
    /// the expenditure requires a deposit asset (<c>true</c>) or not (<c>false</c>).
    /// Returns a validation error if the combination is not recognized.
    /// </summary>
    public static ErrorOr<bool> Validate(string? mainClass, string? subClass)
    {
        if (mainClass is null || !SubClassesByMainClass.TryGetValue(mainClass, out var subClasses))
            return Error.Validation("ExpenditureClass.Invalid", "Unknown expenditure main class.");

        if (subClass is null || !subClasses.Contains(subClass))
            return LocalizableError.Validation("ExpenditureClass.Invalid", "Invalid sub-class for {0}.", mainClass);

        return DepositAssetSubClasses.Contains(subClass);
    }
}
