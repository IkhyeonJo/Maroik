using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Models.ViewModels.Dashboards;

namespace Maroik.Website.Mappers;

/// <summary>
/// Converts a <see cref="DashboardDto"/> returned by <c>IDashboardService</c> into the
/// <see cref="UserIndexOutputViewModel"/> expected by the Dashboard view.
/// Handles response-to-ViewModel mapping only (the year selectors, and the per-category sums/percentages
/// <c>DashboardService</c> already computed via <c>FinanceBreakdownPolicy</c> copied onto their named
/// view-model properties) — no summing or percentage math happens here.
/// </summary>
public static class DashboardViewModelMapper
{
    extension(DashboardDto summary)
    {
        /// <summary>Maps the summary to a fully-populated <see cref="UserIndexOutputViewModel"/>.</summary>
        public UserIndexOutputViewModel ToViewModel()
        {
            var vm = new UserIndexOutputViewModel
            {
                DefaultMonetaryUnit = summary.DefaultMonetaryUnit,
                MonetaryUnits = summary.MonetaryUnits.Where(x => x != null).Select(x => x!),
                StartYear = summary.StartYear,
                EndYear = summary.EndYear,
                SelectedYear = summary.SelectedYear,
                SelectedMonth = summary.SelectedMonth
            };

            if (summary.DefaultMonetaryUnit == null) return vm;

            AssignIncomeBreakdown(vm, summary.YearIncomeBreakdown, summary.YearMonthIncomeBreakdown);
            AssignExpenditureBreakdown(vm, summary.YearExpenditureBreakdown, summary.YearMonthExpenditureBreakdown);

            return vm;
        }
    }

    // ── Breakdown assignment (lookups only — DashboardService already computed every number) ────

    /// <summary>The pre-computed amount of <paramref name="subClass"/> under <paramref name="mainClass"/>, or 0 when absent.</summary>
    private static decimal Amount(IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, string mainClass, string subClass) =>
        breakdown.TryGetValue(mainClass, out var b) ? b.AmountBySubClass.GetValueOrDefault(subClass) : 0m;

    /// <summary>The pre-computed percentage share of <paramref name="subClass"/> under <paramref name="mainClass"/>, or 0 when absent.</summary>
    private static double Pct(IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, string mainClass, string subClass) =>
        breakdown.TryGetValue(mainClass, out var b) ? b.PercentageBySubClass.GetValueOrDefault(subClass) : 0.0;

    /// <summary>
    /// Assigns every "Year" and "YearMonth" income-breakdown view-model property from
    /// <paramref name="yearBreakdown"/> / <paramref name="yearMonthBreakdown"/> respectively.
    /// One call sets both variants of a given main-class/subClass pair via tuple deconstruction, so
    /// the amount/percentage pairs for the two periods cannot drift out of sync.
    /// </summary>
    private static void AssignIncomeBreakdown(
        UserIndexOutputViewModel vm,
        IReadOnlyDictionary<string, FinanceBreakdownDto> yearBreakdown,
        IReadOnlyDictionary<string, FinanceBreakdownDto> yearMonthBreakdown)
    {
        (decimal Year, decimal YearMonth, double PctYear, double PctYearMonth) Values(string mainClass, string subClass) =>
            (Amount(yearBreakdown, mainClass, subClass), Amount(yearMonthBreakdown, mainClass, subClass),
             Pct(yearBreakdown, mainClass, subClass), Pct(yearMonthBreakdown, mainClass, subClass));

        (vm.RegularIncomeLaborIncomeYear, vm.RegularIncomeLaborIncomeYearMonth,
         vm.PercentageOfRegularIncomeLaborIncomeYear, vm.PercentageOfRegularIncomeLaborIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.LaborIncome);
        (vm.RegularIncomeBusinessIncomeYear, vm.RegularIncomeBusinessIncomeYearMonth,
         vm.PercentageOfRegularIncomeBusinessIncomeYear, vm.PercentageOfRegularIncomeBusinessIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.BusinessIncome);
        (vm.RegularIncomePensionIncomeYear, vm.RegularIncomePensionIncomeYearMonth,
         vm.PercentageOfRegularIncomePensionIncomeYear, vm.PercentageOfRegularIncomePensionIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.PensionIncome);
        (vm.RegularIncomeFinancialIncomeYear, vm.RegularIncomeFinancialIncomeYearMonth,
         vm.PercentageOfRegularIncomeFinancialIncomeYear, vm.PercentageOfRegularIncomeFinancialIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.FinancialIncome);
        (vm.RegularIncomeRentalIncomeYear, vm.RegularIncomeRentalIncomeYearMonth,
         vm.PercentageOfRegularIncomeRentalIncomeYear, vm.PercentageOfRegularIncomeRentalIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.RentalIncome);
        (vm.RegularIncomeOtherIncomeYear, vm.RegularIncomeOtherIncomeYearMonth,
         vm.PercentageOfRegularIncomeOtherIncomeYear, vm.PercentageOfRegularIncomeOtherIncomeYearMonth) = Values(IncomeClassNames.RegularIncome, IncomeClassNames.OtherIncome);

        (vm.IrregularIncomeLaborIncomeYear, vm.IrregularIncomeLaborIncomeYearMonth,
         vm.PercentageOfIrregularIncomeLaborIncomeYear, vm.PercentageOfIrregularIncomeLaborIncomeYearMonth) = Values(IncomeClassNames.IrregularIncome, IncomeClassNames.LaborIncome);
        (vm.IrregularIncomeOtherIncomeYear, vm.IrregularIncomeOtherIncomeYearMonth,
         vm.PercentageOfIrregularIncomeOtherIncomeYear, vm.PercentageOfIrregularIncomeOtherIncomeYearMonth) = Values(IncomeClassNames.IrregularIncome, IncomeClassNames.OtherIncome);
    }

    /// <summary>
    /// Assigns every "Year" and "YearMonth" expenditure-breakdown view-model property from
    /// <paramref name="yearBreakdown"/> / <paramref name="yearMonthBreakdown"/> respectively. See
    /// <see cref="AssignIncomeBreakdown"/> for why this sets both periods per call instead of
    /// branching on an <c>isYearMonth</c> flag.
    /// </summary>
    private static void AssignExpenditureBreakdown(
        UserIndexOutputViewModel vm,
        IReadOnlyDictionary<string, FinanceBreakdownDto> yearBreakdown,
        IReadOnlyDictionary<string, FinanceBreakdownDto> yearMonthBreakdown)
    {

        (vm.RegularSavingsDepositYear, vm.RegularSavingsDepositYearMonth,
         vm.PercentageOfRegularSavingsDepositYear, vm.PercentageOfRegularSavingsDepositYearMonth) = Values(ExpenditureClassNames.RegularSavings, ExpenditureClassNames.Deposit);
        (vm.RegularSavingsInvestmentYear, vm.RegularSavingsInvestmentYearMonth,
         vm.PercentageOfRegularSavingsInvestmentYear, vm.PercentageOfRegularSavingsInvestmentYearMonth) = Values(ExpenditureClassNames.RegularSavings, ExpenditureClassNames.Investment);

        (vm.NonConsumerSpendingPublicPensionYear, vm.NonConsumerSpendingPublicPensionYearMonth,
         vm.PercentageOfNonConsumerSpendingPublicPensionYear, vm.PercentageOfNonConsumerSpendingPublicPensionYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.PublicPension);
        (vm.NonConsumerSpendingDebtRepaymentYear, vm.NonConsumerSpendingDebtRepaymentYearMonth,
         vm.PercentageOfNonConsumerSpendingDebtRepaymentYear, vm.PercentageOfNonConsumerSpendingDebtRepaymentYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.DebtRepayment);
        (vm.NonConsumerSpendingTaxYear, vm.NonConsumerSpendingTaxYearMonth,
         vm.PercentageOfNonConsumerSpendingTaxYear, vm.PercentageOfNonConsumerSpendingTaxYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.Tax);
        (vm.NonConsumerSpendingSocialInsuranceYear, vm.NonConsumerSpendingSocialInsuranceYearMonth,
         vm.PercentageOfNonConsumerSpendingSocialInsuranceYear, vm.PercentageOfNonConsumerSpendingSocialInsuranceYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.SocialInsurance);
        (vm.NonConsumerSpendingInterHouseholdTransferExpensesYear, vm.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth,
         vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear, vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.InterHouseholdTransferExpenses);
        (vm.NonConsumerSpendingNonProfitOrganizationTransferYear, vm.NonConsumerSpendingNonProfitOrganizationTransferYearMonth,
         vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear, vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth) = Values(ExpenditureClassNames.NonConsumerSpending, ExpenditureClassNames.NonProfitOrganizationTransfer);

        (vm.ConsumerSpendingMealOrEatOutExpensesYear, vm.ConsumerSpendingMealOrEatOutExpensesYearMonth,
         vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear, vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.MealOrEatOutExpenses);
        (vm.ConsumerSpendingHousingOrSuppliesCostYear, vm.ConsumerSpendingHousingOrSuppliesCostYearMonth,
         vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYear, vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.HousingOrSuppliesCost);
        (vm.ConsumerSpendingEducationExpensesYear, vm.ConsumerSpendingEducationExpensesYearMonth,
         vm.PercentageOfConsumerSpendingEducationExpensesYear, vm.PercentageOfConsumerSpendingEducationExpensesYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.EducationExpenses);
        (vm.ConsumerSpendingMedicalExpensesYear, vm.ConsumerSpendingMedicalExpensesYearMonth,
         vm.PercentageOfConsumerSpendingMedicalExpensesYear, vm.PercentageOfConsumerSpendingMedicalExpensesYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.MedicalExpenses);
        (vm.ConsumerSpendingTransportationCostYear, vm.ConsumerSpendingTransportationCostYearMonth,
         vm.PercentageOfConsumerSpendingTransportationCostYear, vm.PercentageOfConsumerSpendingTransportationCostYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.TransportationCost);
        (vm.ConsumerSpendingCommunicationCostYear, vm.ConsumerSpendingCommunicationCostYearMonth,
         vm.PercentageOfConsumerSpendingCommunicationCostYear, vm.PercentageOfConsumerSpendingCommunicationCostYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.CommunicationCost);
        (vm.ConsumerSpendingLeisureOrCultureYear, vm.ConsumerSpendingLeisureOrCultureYearMonth,
         vm.PercentageOfConsumerSpendingLeisureOrCultureYear, vm.PercentageOfConsumerSpendingLeisureOrCultureYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.LeisureOrCulture);
        (vm.ConsumerSpendingClothingOrShoesYear, vm.ConsumerSpendingClothingOrShoesYearMonth,
         vm.PercentageOfConsumerSpendingClothingOrShoesYear, vm.PercentageOfConsumerSpendingClothingOrShoesYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.ClothingOrShoes);
        (vm.ConsumerSpendingPinMoneyYear, vm.ConsumerSpendingPinMoneyYearMonth,
         vm.PercentageOfConsumerSpendingPinMoneyYear, vm.PercentageOfConsumerSpendingPinMoneyYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.PinMoney);
        (vm.ConsumerSpendingProtectionTypeInsuranceYear, vm.ConsumerSpendingProtectionTypeInsuranceYearMonth,
         vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYear, vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.ProtectionTypeInsurance);
        (vm.ConsumerSpendingOtherExpensesYear, vm.ConsumerSpendingOtherExpensesYearMonth,
         vm.PercentageOfConsumerSpendingOtherExpensesYear, vm.PercentageOfConsumerSpendingOtherExpensesYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.OtherExpenses);
        (vm.ConsumerSpendingUnknownExpenditureYear, vm.ConsumerSpendingUnknownExpenditureYearMonth,
         vm.PercentageOfConsumerSpendingUnknownExpenditureYear, vm.PercentageOfConsumerSpendingUnknownExpenditureYearMonth) = Values(ExpenditureClassNames.ConsumerSpending, ExpenditureClassNames.UnknownExpenditure);
        return;

        (decimal Year, decimal YearMonth, double PctYear, double PctYearMonth) Values(string mainClass, string subClass) =>
            (Amount(yearBreakdown, mainClass, subClass), Amount(yearMonthBreakdown, mainClass, subClass),
             Pct(yearBreakdown, mainClass, subClass), Pct(yearMonthBreakdown, mainClass, subClass));
    }
}
