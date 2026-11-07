using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Contract.Constants;

/// <summary>The expenditure class names, as the finance breakdowns key them, for the code outside the Service layer; each is the name of the Domain's own enum member.</summary>
public static class ExpenditureClassNames
{
    /// <summary><see cref="ExpenditureMainClass.RegularSavings"/>.</summary>
    public const string RegularSavings = nameof(ExpenditureMainClass.RegularSavings);

    /// <summary><see cref="ExpenditureMainClass.NonConsumerSpending"/>.</summary>
    public const string NonConsumerSpending = nameof(ExpenditureMainClass.NonConsumerSpending);

    /// <summary><see cref="ExpenditureMainClass.ConsumerSpending"/>.</summary>
    public const string ConsumerSpending = nameof(ExpenditureMainClass.ConsumerSpending);

    /// <summary><see cref="ExpenditureSubClass.Deposit"/>.</summary>
    public const string Deposit = nameof(ExpenditureSubClass.Deposit);

    /// <summary><see cref="ExpenditureSubClass.Investment"/>.</summary>
    public const string Investment = nameof(ExpenditureSubClass.Investment);

    /// <summary><see cref="ExpenditureSubClass.PublicPension"/>.</summary>
    public const string PublicPension = nameof(ExpenditureSubClass.PublicPension);

    /// <summary><see cref="ExpenditureSubClass.DebtRepayment"/>.</summary>
    public const string DebtRepayment = nameof(ExpenditureSubClass.DebtRepayment);

    /// <summary><see cref="ExpenditureSubClass.Tax"/>.</summary>
    public const string Tax = nameof(ExpenditureSubClass.Tax);

    /// <summary><see cref="ExpenditureSubClass.SocialInsurance"/>.</summary>
    public const string SocialInsurance = nameof(ExpenditureSubClass.SocialInsurance);

    /// <summary><see cref="ExpenditureSubClass.InterHouseholdTransferExpenses"/>.</summary>
    public const string InterHouseholdTransferExpenses = nameof(ExpenditureSubClass.InterHouseholdTransferExpenses);

    /// <summary><see cref="ExpenditureSubClass.NonProfitOrganizationTransfer"/>.</summary>
    public const string NonProfitOrganizationTransfer = nameof(ExpenditureSubClass.NonProfitOrganizationTransfer);

    /// <summary><see cref="ExpenditureSubClass.MealOrEatOutExpenses"/>.</summary>
    public const string MealOrEatOutExpenses = nameof(ExpenditureSubClass.MealOrEatOutExpenses);

    /// <summary><see cref="ExpenditureSubClass.HousingOrSuppliesCost"/>.</summary>
    public const string HousingOrSuppliesCost = nameof(ExpenditureSubClass.HousingOrSuppliesCost);

    /// <summary><see cref="ExpenditureSubClass.EducationExpenses"/>.</summary>
    public const string EducationExpenses = nameof(ExpenditureSubClass.EducationExpenses);

    /// <summary><see cref="ExpenditureSubClass.MedicalExpenses"/>.</summary>
    public const string MedicalExpenses = nameof(ExpenditureSubClass.MedicalExpenses);

    /// <summary><see cref="ExpenditureSubClass.TransportationCost"/>.</summary>
    public const string TransportationCost = nameof(ExpenditureSubClass.TransportationCost);

    /// <summary><see cref="ExpenditureSubClass.CommunicationCost"/>.</summary>
    public const string CommunicationCost = nameof(ExpenditureSubClass.CommunicationCost);

    /// <summary><see cref="ExpenditureSubClass.LeisureOrCulture"/>.</summary>
    public const string LeisureOrCulture = nameof(ExpenditureSubClass.LeisureOrCulture);

    /// <summary><see cref="ExpenditureSubClass.ClothingOrShoes"/>.</summary>
    public const string ClothingOrShoes = nameof(ExpenditureSubClass.ClothingOrShoes);

    /// <summary><see cref="ExpenditureSubClass.PinMoney"/>.</summary>
    public const string PinMoney = nameof(ExpenditureSubClass.PinMoney);

    /// <summary><see cref="ExpenditureSubClass.ProtectionTypeInsurance"/>.</summary>
    public const string ProtectionTypeInsurance = nameof(ExpenditureSubClass.ProtectionTypeInsurance);

    /// <summary><see cref="ExpenditureSubClass.OtherExpenses"/>.</summary>
    public const string OtherExpenses = nameof(ExpenditureSubClass.OtherExpenses);

    /// <summary><see cref="ExpenditureSubClass.UnknownExpenditure"/>.</summary>
    public const string UnknownExpenditure = nameof(ExpenditureSubClass.UnknownExpenditure);
}
