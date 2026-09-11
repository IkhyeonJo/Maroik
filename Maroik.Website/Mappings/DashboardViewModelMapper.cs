using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Website.Models.ViewModels.Dashboard;

namespace Maroik.Website.Mappings;

/// <summary>
/// Converts a <see cref="DashboardDto"/> returned by <c>IDashboardService</c> into the
/// <see cref="UserIndexOutputViewModel"/> expected by the Dashboard view.
/// Handles response-to-ViewModel mapping only (timezone conversion, currency lookup, and copying
/// the per-category sums/percentages <c>DashboardService</c> already computed via
/// <see cref="Maroik.Core.Domain.Finance.FinanceBreakdownPolicy"/> onto their named view-model
/// properties) — no summing or percentage math happens here.
/// </summary>
public static class DashboardViewModelMapper
{
    extension(DashboardDto summary)
    {
        /// <summary>Maps the summary to a fully-populated <see cref="UserIndexOutputViewModel"/>.</summary>
        public UserIndexOutputViewModel ToViewModel(string timeZoneId)
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

            var currencyByProduct = summary.Assets
                .GroupBy(a => a.ProductName ?? "")
                .ToDictionary(g => g.Key, g => g.Last().MonetaryUnit);

            vm.IncomeYearOutputViewModels = MapIncomes(summary.YearIncomes, currencyByProduct, timeZoneId);
            vm.ExpenditureYearOutputViewModels = MapExpenditures(summary.YearExpenditures, currencyByProduct, timeZoneId);
            vm.IncomeYearMonthOutputViewModels = MapIncomes(summary.YearMonthIncomes, currencyByProduct, timeZoneId);
            vm.ExpenditureYearMonthOutputViewModels = MapExpenditures(summary.YearMonthExpenditures, currencyByProduct, timeZoneId);

            AssignIncomeBreakdown(vm, summary.YearIncomeBreakdown, isYearMonth: false);
            AssignIncomeBreakdown(vm, summary.YearMonthIncomeBreakdown, isYearMonth: true);
            AssignExpenditureBreakdown(vm, summary.YearExpenditureBreakdown, isYearMonth: false);
            AssignExpenditureBreakdown(vm, summary.YearMonthExpenditureBreakdown, isYearMonth: true);

            return vm;
        }
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static List<IncomeOutputViewModel> MapIncomes(
        IEnumerable<IncomeResponse> items,
        IReadOnlyDictionary<string, string?> currencyByProduct,
        string timeZoneId)
        => items.Select(item => new IncomeOutputViewModel
        {
            Id = item.Id,
            MainClass = item.MainClass,
            SubClass = item.SubClass,
            Content = item.Content,
            Amount = item.Amount,
            MonetaryUnit = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
            DepositMyAssetProductName = item.DepositMyAssetProductName,
            Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Note = item.Note
        }).ToList();

    private static List<ExpenditureOutputViewModel> MapExpenditures(
        IEnumerable<ExpenditureResponse> items,
        IReadOnlyDictionary<string, string?> currencyByProduct,
        string timeZoneId)
        => items.Select(item => new ExpenditureOutputViewModel
        {
            Id = item.Id,
            MainClass = item.MainClass,
            SubClass = item.SubClass,
            Content = item.Content,
            Amount = item.Amount,
            MonetaryUnit = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
            PaymentMethod = item.PaymentMethod,
            Note = item.Note,
            MyDepositAsset = item.MyDepositAsset,
            Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId)
        }).ToList();

    // ── Breakdown assignment (lookups only — DashboardService already computed every number) ────

    private static decimal Amount(IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, string mainClass, string subClass) =>
        breakdown.TryGetValue(mainClass, out var b) ? b.AmountBySubClass.GetValueOrDefault(subClass) : 0m;

    private static double Pct(IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, string mainClass, string subClass) =>
        breakdown.TryGetValue(mainClass, out var b) ? b.PercentageBySubClass.GetValueOrDefault(subClass) : 0.0;

    private static void AssignIncomeBreakdown(
        UserIndexOutputViewModel vm, IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, bool isYearMonth)
    {
        if (!isYearMonth)
        {
            vm.RegularIncomeLaborIncomeYear = Amount(breakdown, "RegularIncome", "LaborIncome");
            vm.RegularIncomeBusinessIncomeYear = Amount(breakdown, "RegularIncome", "BusinessIncome");
            vm.RegularIncomePensionIncomeYear = Amount(breakdown, "RegularIncome", "PensionIncome");
            vm.RegularIncomeFinancialIncomeYear = Amount(breakdown, "RegularIncome", "FinancialIncome");
            vm.RegularIncomeRentalIncomeYear = Amount(breakdown, "RegularIncome", "RentalIncome");
            vm.RegularIncomeOtherIncomeYear = Amount(breakdown, "RegularIncome", "OtherIncome");
            vm.PercentageOfRegularIncomeLaborIncomeYear = Pct(breakdown, "RegularIncome", "LaborIncome");
            vm.PercentageOfRegularIncomeBusinessIncomeYear = Pct(breakdown, "RegularIncome", "BusinessIncome");
            vm.PercentageOfRegularIncomePensionIncomeYear = Pct(breakdown, "RegularIncome", "PensionIncome");
            vm.PercentageOfRegularIncomeFinancialIncomeYear = Pct(breakdown, "RegularIncome", "FinancialIncome");
            vm.PercentageOfRegularIncomeRentalIncomeYear = Pct(breakdown, "RegularIncome", "RentalIncome");
            vm.PercentageOfRegularIncomeOtherIncomeYear = Pct(breakdown, "RegularIncome", "OtherIncome");

            vm.IrregularIncomeLaborIncomeYear = Amount(breakdown, "IrregularIncome", "LaborIncome");
            vm.IrregularIncomeOtherIncomeYear = Amount(breakdown, "IrregularIncome", "OtherIncome");
            vm.PercentageOfIrregularIncomeLaborIncomeYear = Pct(breakdown, "IrregularIncome", "LaborIncome");
            vm.PercentageOfIrregularIncomeOtherIncomeYear = Pct(breakdown, "IrregularIncome", "OtherIncome");
        }
        else
        {
            vm.RegularIncomeLaborIncomeYearMonth = Amount(breakdown, "RegularIncome", "LaborIncome");
            vm.RegularIncomeBusinessIncomeYearMonth = Amount(breakdown, "RegularIncome", "BusinessIncome");
            vm.RegularIncomePensionIncomeYearMonth = Amount(breakdown, "RegularIncome", "PensionIncome");
            vm.RegularIncomeFinancialIncomeYearMonth = Amount(breakdown, "RegularIncome", "FinancialIncome");
            vm.RegularIncomeRentalIncomeYearMonth = Amount(breakdown, "RegularIncome", "RentalIncome");
            vm.RegularIncomeOtherIncomeYearMonth = Amount(breakdown, "RegularIncome", "OtherIncome");
            vm.PercentageOfRegularIncomeLaborIncomeYearMonth = Pct(breakdown, "RegularIncome", "LaborIncome");
            vm.PercentageOfRegularIncomeBusinessIncomeYearMonth = Pct(breakdown, "RegularIncome", "BusinessIncome");
            vm.PercentageOfRegularIncomePensionIncomeYearMonth = Pct(breakdown, "RegularIncome", "PensionIncome");
            vm.PercentageOfRegularIncomeFinancialIncomeYearMonth = Pct(breakdown, "RegularIncome", "FinancialIncome");
            vm.PercentageOfRegularIncomeRentalIncomeYearMonth = Pct(breakdown, "RegularIncome", "RentalIncome");
            vm.PercentageOfRegularIncomeOtherIncomeYearMonth = Pct(breakdown, "RegularIncome", "OtherIncome");

            vm.IrregularIncomeLaborIncomeYearMonth = Amount(breakdown, "IrregularIncome", "LaborIncome");
            vm.IrregularIncomeOtherIncomeYearMonth = Amount(breakdown, "IrregularIncome", "OtherIncome");
            vm.PercentageOfIrregularIncomeLaborIncomeYearMonth = Pct(breakdown, "IrregularIncome", "LaborIncome");
            vm.PercentageOfIrregularIncomeOtherIncomeYearMonth = Pct(breakdown, "IrregularIncome", "OtherIncome");
        }
    }

    private static void AssignExpenditureBreakdown(
        UserIndexOutputViewModel vm, IReadOnlyDictionary<string, FinanceBreakdownDto> breakdown, bool isYearMonth)
    {
        if (!isYearMonth)
        {
            vm.RegularSavingsDepositYear = Amount(breakdown, "RegularSavings", "Deposit");
            vm.RegularSavingsInvestmentYear = Amount(breakdown, "RegularSavings", "Investment");
            vm.PercentageOfRegularSavingsDepositYear = Pct(breakdown, "RegularSavings", "Deposit");
            vm.PercentageOfRegularSavingsInvestmentYear = Pct(breakdown, "RegularSavings", "Investment");

            vm.NonConsumerSpendingPublicPensionYear = Amount(breakdown, "NonConsumerSpending", "PublicPension");
            vm.NonConsumerSpendingDebtRepaymentYear = Amount(breakdown, "NonConsumerSpending", "DebtRepayment");
            vm.NonConsumerSpendingTaxYear = Amount(breakdown, "NonConsumerSpending", "Tax");
            vm.NonConsumerSpendingSocialInsuranceYear = Amount(breakdown, "NonConsumerSpending", "SocialInsurance");
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYear = Amount(breakdown, "NonConsumerSpending", "InterHouseholdTransferExpenses");
            vm.NonConsumerSpendingNonProfitOrganizationTransferYear = Amount(breakdown, "NonConsumerSpending", "NonProfitOrganizationTransfer");
            vm.PercentageOfNonConsumerSpendingPublicPensionYear = Pct(breakdown, "NonConsumerSpending", "PublicPension");
            vm.PercentageOfNonConsumerSpendingDebtRepaymentYear = Pct(breakdown, "NonConsumerSpending", "DebtRepayment");
            vm.PercentageOfNonConsumerSpendingTaxYear = Pct(breakdown, "NonConsumerSpending", "Tax");
            vm.PercentageOfNonConsumerSpendingSocialInsuranceYear = Pct(breakdown, "NonConsumerSpending", "SocialInsurance");
            vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear = Pct(breakdown, "NonConsumerSpending", "InterHouseholdTransferExpenses");
            vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear = Pct(breakdown, "NonConsumerSpending", "NonProfitOrganizationTransfer");

            vm.ConsumerSpendingMealOrEatOutExpensesYear = Amount(breakdown, "ConsumerSpending", "MealOrEatOutExpenses");
            vm.ConsumerSpendingHousingOrSuppliesCostYear = Amount(breakdown, "ConsumerSpending", "HousingOrSuppliesCost");
            vm.ConsumerSpendingEducationExpensesYear = Amount(breakdown, "ConsumerSpending", "EducationExpenses");
            vm.ConsumerSpendingMedicalExpensesYear = Amount(breakdown, "ConsumerSpending", "MedicalExpenses");
            vm.ConsumerSpendingTransportationCostYear = Amount(breakdown, "ConsumerSpending", "TransportationCost");
            vm.ConsumerSpendingCommunicationCostYear = Amount(breakdown, "ConsumerSpending", "CommunicationCost");
            vm.ConsumerSpendingLeisureOrCultureYear = Amount(breakdown, "ConsumerSpending", "LeisureOrCulture");
            vm.ConsumerSpendingClothingOrShoesYear = Amount(breakdown, "ConsumerSpending", "ClothingOrShoes");
            vm.ConsumerSpendingPinMoneyYear = Amount(breakdown, "ConsumerSpending", "PinMoney");
            vm.ConsumerSpendingProtectionTypeInsuranceYear = Amount(breakdown, "ConsumerSpending", "ProtectionTypeInsurance");
            vm.ConsumerSpendingOtherExpensesYear = Amount(breakdown, "ConsumerSpending", "OtherExpenses");
            vm.ConsumerSpendingUnknownExpenditureYear = Amount(breakdown, "ConsumerSpending", "UnknownExpenditure");
            vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear = Pct(breakdown, "ConsumerSpending", "MealOrEatOutExpenses");
            vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYear = Pct(breakdown, "ConsumerSpending", "HousingOrSuppliesCost");
            vm.PercentageOfConsumerSpendingEducationExpensesYear = Pct(breakdown, "ConsumerSpending", "EducationExpenses");
            vm.PercentageOfConsumerSpendingMedicalExpensesYear = Pct(breakdown, "ConsumerSpending", "MedicalExpenses");
            vm.PercentageOfConsumerSpendingTransportationCostYear = Pct(breakdown, "ConsumerSpending", "TransportationCost");
            vm.PercentageOfConsumerSpendingCommunicationCostYear = Pct(breakdown, "ConsumerSpending", "CommunicationCost");
            vm.PercentageOfConsumerSpendingLeisureOrCultureYear = Pct(breakdown, "ConsumerSpending", "LeisureOrCulture");
            vm.PercentageOfConsumerSpendingClothingOrShoesYear = Pct(breakdown, "ConsumerSpending", "ClothingOrShoes");
            vm.PercentageOfConsumerSpendingPinMoneyYear = Pct(breakdown, "ConsumerSpending", "PinMoney");
            vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYear = Pct(breakdown, "ConsumerSpending", "ProtectionTypeInsurance");
            vm.PercentageOfConsumerSpendingOtherExpensesYear = Pct(breakdown, "ConsumerSpending", "OtherExpenses");
            vm.PercentageOfConsumerSpendingUnknownExpenditureYear = Pct(breakdown, "ConsumerSpending", "UnknownExpenditure");
        }
        else
        {
            vm.RegularSavingsDepositYearMonth = Amount(breakdown, "RegularSavings", "Deposit");
            vm.RegularSavingsInvestmentYearMonth = Amount(breakdown, "RegularSavings", "Investment");
            vm.PercentageOfRegularSavingsDepositYearMonth = Pct(breakdown, "RegularSavings", "Deposit");
            vm.PercentageOfRegularSavingsInvestmentYearMonth = Pct(breakdown, "RegularSavings", "Investment");

            vm.NonConsumerSpendingPublicPensionYearMonth = Amount(breakdown, "NonConsumerSpending", "PublicPension");
            vm.NonConsumerSpendingDebtRepaymentYearMonth = Amount(breakdown, "NonConsumerSpending", "DebtRepayment");
            vm.NonConsumerSpendingTaxYearMonth = Amount(breakdown, "NonConsumerSpending", "Tax");
            vm.NonConsumerSpendingSocialInsuranceYearMonth = Amount(breakdown, "NonConsumerSpending", "SocialInsurance");
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth = Amount(breakdown, "NonConsumerSpending", "InterHouseholdTransferExpenses");
            vm.NonConsumerSpendingNonProfitOrganizationTransferYearMonth = Amount(breakdown, "NonConsumerSpending", "NonProfitOrganizationTransfer");
            vm.PercentageOfNonConsumerSpendingPublicPensionYearMonth = Pct(breakdown, "NonConsumerSpending", "PublicPension");
            vm.PercentageOfNonConsumerSpendingDebtRepaymentYearMonth = Pct(breakdown, "NonConsumerSpending", "DebtRepayment");
            vm.PercentageOfNonConsumerSpendingTaxYearMonth = Pct(breakdown, "NonConsumerSpending", "Tax");
            vm.PercentageOfNonConsumerSpendingSocialInsuranceYearMonth = Pct(breakdown, "NonConsumerSpending", "SocialInsurance");
            vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth = Pct(breakdown, "NonConsumerSpending", "InterHouseholdTransferExpenses");
            vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth = Pct(breakdown, "NonConsumerSpending", "NonProfitOrganizationTransfer");

            vm.ConsumerSpendingMealOrEatOutExpensesYearMonth = Amount(breakdown, "ConsumerSpending", "MealOrEatOutExpenses");
            vm.ConsumerSpendingHousingOrSuppliesCostYearMonth = Amount(breakdown, "ConsumerSpending", "HousingOrSuppliesCost");
            vm.ConsumerSpendingEducationExpensesYearMonth = Amount(breakdown, "ConsumerSpending", "EducationExpenses");
            vm.ConsumerSpendingMedicalExpensesYearMonth = Amount(breakdown, "ConsumerSpending", "MedicalExpenses");
            vm.ConsumerSpendingTransportationCostYearMonth = Amount(breakdown, "ConsumerSpending", "TransportationCost");
            vm.ConsumerSpendingCommunicationCostYearMonth = Amount(breakdown, "ConsumerSpending", "CommunicationCost");
            vm.ConsumerSpendingLeisureOrCultureYearMonth = Amount(breakdown, "ConsumerSpending", "LeisureOrCulture");
            vm.ConsumerSpendingClothingOrShoesYearMonth = Amount(breakdown, "ConsumerSpending", "ClothingOrShoes");
            vm.ConsumerSpendingPinMoneyYearMonth = Amount(breakdown, "ConsumerSpending", "PinMoney");
            vm.ConsumerSpendingProtectionTypeInsuranceYearMonth = Amount(breakdown, "ConsumerSpending", "ProtectionTypeInsurance");
            vm.ConsumerSpendingOtherExpensesYearMonth = Amount(breakdown, "ConsumerSpending", "OtherExpenses");
            vm.ConsumerSpendingUnknownExpenditureYearMonth = Amount(breakdown, "ConsumerSpending", "UnknownExpenditure");
            vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth = Pct(breakdown, "ConsumerSpending", "MealOrEatOutExpenses");
            vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth = Pct(breakdown, "ConsumerSpending", "HousingOrSuppliesCost");
            vm.PercentageOfConsumerSpendingEducationExpensesYearMonth = Pct(breakdown, "ConsumerSpending", "EducationExpenses");
            vm.PercentageOfConsumerSpendingMedicalExpensesYearMonth = Pct(breakdown, "ConsumerSpending", "MedicalExpenses");
            vm.PercentageOfConsumerSpendingTransportationCostYearMonth = Pct(breakdown, "ConsumerSpending", "TransportationCost");
            vm.PercentageOfConsumerSpendingCommunicationCostYearMonth = Pct(breakdown, "ConsumerSpending", "CommunicationCost");
            vm.PercentageOfConsumerSpendingLeisureOrCultureYearMonth = Pct(breakdown, "ConsumerSpending", "LeisureOrCulture");
            vm.PercentageOfConsumerSpendingClothingOrShoesYearMonth = Pct(breakdown, "ConsumerSpending", "ClothingOrShoes");
            vm.PercentageOfConsumerSpendingPinMoneyYearMonth = Pct(breakdown, "ConsumerSpending", "PinMoney");
            vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth = Pct(breakdown, "ConsumerSpending", "ProtectionTypeInsurance");
            vm.PercentageOfConsumerSpendingOtherExpensesYearMonth = Pct(breakdown, "ConsumerSpending", "OtherExpenses");
            vm.PercentageOfConsumerSpendingUnknownExpenditureYearMonth = Pct(breakdown, "ConsumerSpending", "UnknownExpenditure");
        }
    }
}
