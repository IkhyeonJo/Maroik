namespace Maroik.WebSite.Models.ViewModels.DashBoard;

public class UserIndexOutputViewModel
{
    public List<IncomeOutputViewModel> IncomeYearOutputViewModels { get; set; } = [];
    public List<ExpenditureOutputViewModel> ExpenditureYearOutputViewModels { get; set; } = [];
    public List<IncomeOutputViewModel> IncomeYearMonthOutputViewModels { get; set; } = [];
    public List<ExpenditureOutputViewModel> ExpenditureYearMonthOutputViewModels { get; set; } = [];
    public IEnumerable<string> MonetaryUnits { get; set; } = [];
    public string DefaultMonetaryUnit { get; set; }

    // Year/Month selectors
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }

    // Year Income - RegularIncome
    public decimal RegularIncomeLaborIncomeYear { get; set; }
    public decimal RegularIncomeBusinessIncomeYear { get; set; }
    public decimal RegularIncomePensionIncomeYear { get; set; }
    public decimal RegularIncomeFinancialIncomeYear { get; set; }
    public decimal RegularIncomeRentalIncomeYear { get; set; }
    public decimal RegularIncomeOtherIncomeYear { get; set; }
    public double PercentageOfRegularIncomeLaborIncomeYear { get; set; }
    public double PercentageOfRegularIncomeBusinessIncomeYear { get; set; }
    public double PercentageOfRegularIncomePensionIncomeYear { get; set; }
    public double PercentageOfRegularIncomeFinancialIncomeYear { get; set; }
    public double PercentageOfRegularIncomeRentalIncomeYear { get; set; }
    public double PercentageOfRegularIncomeOtherIncomeYear { get; set; }

    // Year Income - IrregularIncome
    public decimal IrregularIncomeLaborIncomeYear { get; set; }
    public decimal IrregularIncomeOtherIncomeYear { get; set; }
    public double PercentageOfIrregularIncomeLaborIncomeYear { get; set; }
    public double PercentageOfIrregularIncomeOtherIncomeYear { get; set; }

    // Year Expenditure - RegularSavings
    public decimal RegularSavingsDepositYear { get; set; }
    public decimal RegularSavingsInvestmentYear { get; set; }
    public double PercentageOfRegularSavingsDepositYear { get; set; }
    public double PercentageOfRegularSavingsInvestmentYear { get; set; }

    // Year Expenditure - NonConsumerSpending
    public decimal NonConsumerSpendingPublicPensionYear { get; set; }
    public decimal NonConsumerSpendingDebtRepaymentYear { get; set; }
    public decimal NonConsumerSpendingTaxYear { get; set; }
    public decimal NonConsumerSpendingSocialInsuranceYear { get; set; }
    public decimal NonConsumerSpendingInterHouseholdTransferExpensesYear { get; set; }
    public decimal NonConsumerSpendingNonProfitOrganizationTransferYear { get; set; }
    public double PercentageOfNonConsumerSpendingPublicPensionYear { get; set; }
    public double PercentageOfNonConsumerSpendingDebtRepaymentYear { get; set; }
    public double PercentageOfNonConsumerSpendingTaxYear { get; set; }
    public double PercentageOfNonConsumerSpendingSocialInsuranceYear { get; set; }
    public double PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear { get; set; }
    public double PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear { get; set; }

    // Year Expenditure - ConsumerSpending
    public decimal ConsumerSpendingMealOrEatOutExpensesYear { get; set; }
    public decimal ConsumerSpendingHousingOrSuppliesCostYear { get; set; }
    public decimal ConsumerSpendingEducationExpensesYear { get; set; }
    public decimal ConsumerSpendingMedicalExpensesYear { get; set; }
    public decimal ConsumerSpendingTransportationCostYear { get; set; }
    public decimal ConsumerSpendingCommunicationCostYear { get; set; }
    public decimal ConsumerSpendingLeisureOrCultureYear { get; set; }
    public decimal ConsumerSpendingClothingOrShoesYear { get; set; }
    public decimal ConsumerSpendingPinMoneyYear { get; set; }
    public decimal ConsumerSpendingProtectionTypeInsuranceYear { get; set; }
    public decimal ConsumerSpendingOtherExpensesYear { get; set; }
    public decimal ConsumerSpendingUnknownExpenditureYear { get; set; }
    public double PercentageOfConsumerSpendingMealOrEatOutExpensesYear { get; set; }
    public double PercentageOfConsumerSpendingHousingOrSuppliesCostYear { get; set; }
    public double PercentageOfConsumerSpendingEducationExpensesYear { get; set; }
    public double PercentageOfConsumerSpendingMedicalExpensesYear { get; set; }
    public double PercentageOfConsumerSpendingTransportationCostYear { get; set; }
    public double PercentageOfConsumerSpendingCommunicationCostYear { get; set; }
    public double PercentageOfConsumerSpendingLeisureOrCultureYear { get; set; }
    public double PercentageOfConsumerSpendingClothingOrShoesYear { get; set; }
    public double PercentageOfConsumerSpendingPinMoneyYear { get; set; }
    public double PercentageOfConsumerSpendingProtectionTypeInsuranceYear { get; set; }
    public double PercentageOfConsumerSpendingOtherExpensesYear { get; set; }
    public double PercentageOfConsumerSpendingUnknownExpenditureYear { get; set; }

    // YearMonth Income - RegularIncome
    public decimal RegularIncomeLaborIncomeYearMonth { get; set; }
    public decimal RegularIncomeBusinessIncomeYearMonth { get; set; }
    public decimal RegularIncomePensionIncomeYearMonth { get; set; }
    public decimal RegularIncomeFinancialIncomeYearMonth { get; set; }
    public decimal RegularIncomeRentalIncomeYearMonth { get; set; }
    public decimal RegularIncomeOtherIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomeLaborIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomeBusinessIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomePensionIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomeFinancialIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomeRentalIncomeYearMonth { get; set; }
    public double PercentageOfRegularIncomeOtherIncomeYearMonth { get; set; }

    // YearMonth Income - IrregularIncome
    public decimal IrregularIncomeLaborIncomeYearMonth { get; set; }
    public decimal IrregularIncomeOtherIncomeYearMonth { get; set; }
    public double PercentageOfIrregularIncomeLaborIncomeYearMonth { get; set; }
    public double PercentageOfIrregularIncomeOtherIncomeYearMonth { get; set; }

    // YearMonth Expenditure - RegularSavings
    public decimal RegularSavingsDepositYearMonth { get; set; }
    public decimal RegularSavingsInvestmentYearMonth { get; set; }
    public double PercentageOfRegularSavingsDepositYearMonth { get; set; }
    public double PercentageOfRegularSavingsInvestmentYearMonth { get; set; }

    // YearMonth Expenditure - NonConsumerSpending
    public decimal NonConsumerSpendingPublicPensionYearMonth { get; set; }
    public decimal NonConsumerSpendingDebtRepaymentYearMonth { get; set; }
    public decimal NonConsumerSpendingTaxYearMonth { get; set; }
    public decimal NonConsumerSpendingSocialInsuranceYearMonth { get; set; }
    public decimal NonConsumerSpendingInterHouseholdTransferExpensesYearMonth { get; set; }
    public decimal NonConsumerSpendingNonProfitOrganizationTransferYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingPublicPensionYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingDebtRepaymentYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingTaxYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingSocialInsuranceYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth { get; set; }
    public double PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth { get; set; }

    // YearMonth Expenditure - ConsumerSpending
    public decimal ConsumerSpendingMealOrEatOutExpensesYearMonth { get; set; }
    public decimal ConsumerSpendingHousingOrSuppliesCostYearMonth { get; set; }
    public decimal ConsumerSpendingEducationExpensesYearMonth { get; set; }
    public decimal ConsumerSpendingMedicalExpensesYearMonth { get; set; }
    public decimal ConsumerSpendingTransportationCostYearMonth { get; set; }
    public decimal ConsumerSpendingCommunicationCostYearMonth { get; set; }
    public decimal ConsumerSpendingLeisureOrCultureYearMonth { get; set; }
    public decimal ConsumerSpendingClothingOrShoesYearMonth { get; set; }
    public decimal ConsumerSpendingPinMoneyYearMonth { get; set; }
    public decimal ConsumerSpendingProtectionTypeInsuranceYearMonth { get; set; }
    public decimal ConsumerSpendingOtherExpensesYearMonth { get; set; }
    public decimal ConsumerSpendingUnknownExpenditureYearMonth { get; set; }
    public double PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth { get; set; }
    public double PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth { get; set; }
    public double PercentageOfConsumerSpendingEducationExpensesYearMonth { get; set; }
    public double PercentageOfConsumerSpendingMedicalExpensesYearMonth { get; set; }
    public double PercentageOfConsumerSpendingTransportationCostYearMonth { get; set; }
    public double PercentageOfConsumerSpendingCommunicationCostYearMonth { get; set; }
    public double PercentageOfConsumerSpendingLeisureOrCultureYearMonth { get; set; }
    public double PercentageOfConsumerSpendingClothingOrShoesYearMonth { get; set; }
    public double PercentageOfConsumerSpendingPinMoneyYearMonth { get; set; }
    public double PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth { get; set; }
    public double PercentageOfConsumerSpendingOtherExpensesYearMonth { get; set; }
    public double PercentageOfConsumerSpendingUnknownExpenditureYearMonth { get; set; }
}
