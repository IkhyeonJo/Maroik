using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels.DashBoard;

namespace Maroik.WebSite.Mappings;

/// <summary>
/// Converts a <see cref="DashboardSummary"/> returned by <c>IDashboardService</c> into the
/// <see cref="UserIndexOutputViewModel"/> expected by the Dashboard view.
/// Handles response-to-ViewModel mapping (with timezone conversion and currency lookup)
/// and all per-category aggregation and percentage calculations, keeping this logic
/// out of the controller.
/// </summary>
public static class DashboardViewModelMapper
{
    /// <summary>Maps <paramref name="summary"/> to a fully-populated <see cref="UserIndexOutputViewModel"/>.</summary>
    public static UserIndexOutputViewModel ToViewModel(this DashboardSummary summary, string timeZoneId)
    {
        var vm = new UserIndexOutputViewModel
        {
            DefaultMonetaryUnit = summary.DefaultMonetaryUnit,
            MonetaryUnits       = summary.MonetaryUnits?.Where(x => x != null).Select(x => x!) ?? [],
            StartYear           = summary.StartYear,
            EndYear             = summary.EndYear,
            SelectedYear        = summary.SelectedYear,
            SelectedMonth       = summary.SelectedMonth
        };

        if (summary.DefaultMonetaryUnit == null) return vm;

        var currencyByProduct = summary.Assets
            .ToDictionary(a => a.ProductName ?? "", a => (string?)a.MonetaryUnit);

        vm.IncomeYearOutputViewModels           = MapIncomes(summary.YearIncomes,           currencyByProduct, timeZoneId);
        vm.ExpenditureYearOutputViewModels      = MapExpenditures(summary.YearExpenditures, currencyByProduct, timeZoneId);
        vm.IncomeYearMonthOutputViewModels      = MapIncomes(summary.YearMonthIncomes,      currencyByProduct, timeZoneId);
        vm.ExpenditureYearMonthOutputViewModels = MapExpenditures(summary.YearMonthExpenditures, currencyByProduct, timeZoneId);

        AggregateIncomes(vm);
        AggregateExpenditures(vm);

        return vm;
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static List<IncomeOutputViewModel> MapIncomes(
        IEnumerable<IncomeResponse> items,
        IReadOnlyDictionary<string, string?> currencyByProduct,
        string timeZoneId)
        => items.Select(item => new IncomeOutputViewModel
        {
            Id                        = item.Id,
            MainClass                 = item.MainClass,
            SubClass                  = item.SubClass,
            Content                   = item.Content,
            Amount                    = item.Amount,
            MonetaryUnit              = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
            DepositMyAssetProductName = item.DepositMyAssetProductName,
            Created                   = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Updated                   = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Note                      = item.Note
        }).ToList();

    private static List<ExpenditureOutputViewModel> MapExpenditures(
        IEnumerable<ExpenditureResponse> items,
        IReadOnlyDictionary<string, string?> currencyByProduct,
        string timeZoneId)
        => items.Select(item => new ExpenditureOutputViewModel
        {
            Id             = item.Id,
            MainClass      = item.MainClass,
            SubClass       = item.SubClass,
            Content        = item.Content,
            Amount         = item.Amount,
            MonetaryUnit   = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
            PaymentMethod  = item.PaymentMethod,
            Note           = item.Note,
            MyDepositAsset = item.MyDepositAsset,
            Created        = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneId),
            Updated        = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneId)
        }).ToList();

    // ── Aggregation ──────────────────────────────────────────────────────────

    private static void AggregateIncomes(UserIndexOutputViewModel vm)
    {
        AggregatePeriodIncomes(vm, vm.IncomeYearOutputViewModels, isYearMonth: false);
        AggregatePeriodIncomes(vm, vm.IncomeYearMonthOutputViewModels, isYearMonth: true);
    }

    private static void AggregatePeriodIncomes(UserIndexOutputViewModel vm, List<IncomeOutputViewModel> incomes, bool isYearMonth)
    {
        var regular   = incomes.Where(x => x.MainClass == "RegularIncome").ToList();
        var irregular = incomes.Where(x => x.MainClass == "IrregularIncome").ToList();

        decimal regularLabor    = SumSub(regular,   "LaborIncome");
        decimal regularBusiness = SumSub(regular,   "BusinessIncome");
        decimal regularPension  = SumSub(regular,   "PensionIncome");
        decimal regularFinancial = SumSub(regular,  "FinancialIncome");
        decimal regularRental   = SumSub(regular,   "RentalIncome");
        decimal regularOther    = SumSub(regular,   "OtherIncome");
        decimal totalRegular    = regular.Sum(v => v.Amount);

        decimal irregularLabor = SumSub(irregular, "LaborIncome");
        decimal irregularOther = SumSub(irregular, "OtherIncome");
        decimal totalIrregular = irregular.Sum(v => v.Amount);

        if (!isYearMonth)
        {
            vm.RegularIncomeLaborIncomeYear    = regularLabor;
            vm.RegularIncomeBusinessIncomeYear = regularBusiness;
            vm.RegularIncomePensionIncomeYear  = regularPension;
            vm.RegularIncomeFinancialIncomeYear = regularFinancial;
            vm.RegularIncomeRentalIncomeYear   = regularRental;
            vm.RegularIncomeOtherIncomeYear    = regularOther;
            if (totalRegular != 0)
            {
                vm.PercentageOfRegularIncomeLaborIncomeYear    = Pct(regularLabor,    totalRegular);
                vm.PercentageOfRegularIncomeBusinessIncomeYear = Pct(regularBusiness, totalRegular);
                vm.PercentageOfRegularIncomePensionIncomeYear  = Pct(regularPension,  totalRegular);
                vm.PercentageOfRegularIncomeFinancialIncomeYear = Pct(regularFinancial, totalRegular);
                vm.PercentageOfRegularIncomeRentalIncomeYear   = Pct(regularRental,   totalRegular);
                vm.PercentageOfRegularIncomeOtherIncomeYear    = Pct(regularOther,    totalRegular);
            }
            vm.IrregularIncomeLaborIncomeYear = irregularLabor;
            vm.IrregularIncomeOtherIncomeYear = irregularOther;
            if (totalIrregular != 0)
            {
                vm.PercentageOfIrregularIncomeLaborIncomeYear = Pct(irregularLabor, totalIrregular);
                vm.PercentageOfIrregularIncomeOtherIncomeYear = Pct(irregularOther, totalIrregular);
            }
        }
        else
        {
            vm.RegularIncomeLaborIncomeYearMonth    = regularLabor;
            vm.RegularIncomeBusinessIncomeYearMonth = regularBusiness;
            vm.RegularIncomePensionIncomeYearMonth  = regularPension;
            vm.RegularIncomeFinancialIncomeYearMonth = regularFinancial;
            vm.RegularIncomeRentalIncomeYearMonth   = regularRental;
            vm.RegularIncomeOtherIncomeYearMonth    = regularOther;
            if (totalRegular != 0)
            {
                vm.PercentageOfRegularIncomeLaborIncomeYearMonth    = Pct(regularLabor,    totalRegular);
                vm.PercentageOfRegularIncomeBusinessIncomeYearMonth = Pct(regularBusiness, totalRegular);
                vm.PercentageOfRegularIncomePensionIncomeYearMonth  = Pct(regularPension,  totalRegular);
                vm.PercentageOfRegularIncomeFinancialIncomeYearMonth = Pct(regularFinancial, totalRegular);
                vm.PercentageOfRegularIncomeRentalIncomeYearMonth   = Pct(regularRental,   totalRegular);
                vm.PercentageOfRegularIncomeOtherIncomeYearMonth    = Pct(regularOther,    totalRegular);
            }
            vm.IrregularIncomeLaborIncomeYearMonth = irregularLabor;
            vm.IrregularIncomeOtherIncomeYearMonth = irregularOther;
            if (totalIrregular != 0)
            {
                vm.PercentageOfIrregularIncomeLaborIncomeYearMonth = Pct(irregularLabor, totalIrregular);
                vm.PercentageOfIrregularIncomeOtherIncomeYearMonth = Pct(irregularOther, totalIrregular);
            }
        }
    }

    private static void AggregateExpenditures(UserIndexOutputViewModel vm)
    {
        AggregatePeriodExpenditures(vm, vm.ExpenditureYearOutputViewModels,      isYearMonth: false);
        AggregatePeriodExpenditures(vm, vm.ExpenditureYearMonthOutputViewModels, isYearMonth: true);
    }

    private static void AggregatePeriodExpenditures(UserIndexOutputViewModel vm, List<ExpenditureOutputViewModel> expenditures, bool isYearMonth)
    {
        var savings     = expenditures.Where(x => x.MainClass == "RegularSavings").ToList();
        var nonConsumer = expenditures.Where(x => x.MainClass == "NonConsumerSpending").ToList();
        var consumer    = expenditures.Where(x => x.MainClass == "ConsumerSpending").ToList();

        decimal savingsDeposit    = SumSub(savings, "Deposit");
        decimal savingsInvestment = SumSub(savings, "Investment");
        decimal totalSavings      = savings.Sum(v => v.Amount);

        decimal ncPublicPension     = SumSub(nonConsumer, "PublicPension");
        decimal ncDebt              = SumSub(nonConsumer, "DebtRepayment");
        decimal ncTax               = SumSub(nonConsumer, "Tax");
        decimal ncSocial            = SumSub(nonConsumer, "SocialInsurance");
        decimal ncInterHousehold    = SumSub(nonConsumer, "InterHouseholdTranserExpenses");
        decimal ncNonProfit         = SumSub(nonConsumer, "NonProfitOrganizationTransfer");
        decimal totalNonConsumer    = nonConsumer.Sum(v => v.Amount);

        decimal cMeal      = SumSub(consumer, "MealOrEatOutExpenses");
        decimal cHousing   = SumSub(consumer, "HousingOrSuppliesCost");
        decimal cEducation = SumSub(consumer, "EducationExpenses");
        decimal cMedical   = SumSub(consumer, "MedicalExpenses");
        decimal cTransport = SumSub(consumer, "TransportationCost");
        decimal cComm      = SumSub(consumer, "CommunicationCost");
        decimal cLeisure   = SumSub(consumer, "LeisureOrCulture");
        decimal cClothing  = SumSub(consumer, "ClothingOrShoes");
        decimal cPin       = SumSub(consumer, "PinMoney");
        decimal cInsurance = SumSub(consumer, "ProtectionTypeInsurance");
        decimal cOther     = SumSub(consumer, "OtherExpenses");
        decimal cUnknown   = SumSub(consumer, "UnknownExpenditure");
        decimal totalConsumer = consumer.Sum(v => v.Amount);

        if (!isYearMonth)
        {
            vm.RegularSavingsDepositYear    = savingsDeposit;
            vm.RegularSavingsInvestmentYear = savingsInvestment;
            if (totalSavings != 0) { vm.PercentageOfRegularSavingsDepositYear = Pct(savingsDeposit, totalSavings); vm.PercentageOfRegularSavingsInvestmentYear = Pct(savingsInvestment, totalSavings); }

            vm.NonConsumerSpendingPublicPensionYear                    = ncPublicPension;
            vm.NonConsumerSpendingDebtRepaymentYear                    = ncDebt;
            vm.NonConsumerSpendingTaxYear                              = ncTax;
            vm.NonConsumerSpendingSocialInsuranceYear                  = ncSocial;
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYear   = ncInterHousehold;
            vm.NonConsumerSpendingNonProfitOrganizationTransferYear    = ncNonProfit;
            if (totalNonConsumer != 0) { vm.PercentageOfNonConsumerSpendingPublicPensionYear = Pct(ncPublicPension, totalNonConsumer); vm.PercentageOfNonConsumerSpendingDebtRepaymentYear = Pct(ncDebt, totalNonConsumer); vm.PercentageOfNonConsumerSpendingTaxYear = Pct(ncTax, totalNonConsumer); vm.PercentageOfNonConsumerSpendingSocialInsuranceYear = Pct(ncSocial, totalNonConsumer); vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear = Pct(ncInterHousehold, totalNonConsumer); vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYear = Pct(ncNonProfit, totalNonConsumer); }

            vm.ConsumerSpendingMealOrEatOutExpensesYear       = cMeal;
            vm.ConsumerSpendingHousingOrSuppliesCostYear      = cHousing;
            vm.ConsumerSpendingEducationExpensesYear          = cEducation;
            vm.ConsumerSpendingMedicalExpensesYear            = cMedical;
            vm.ConsumerSpendingTransportationCostYear         = cTransport;
            vm.ConsumerSpendingCommunicationCostYear          = cComm;
            vm.ConsumerSpendingLeisureOrCultureYear           = cLeisure;
            vm.ConsumerSpendingClothingOrShoesYear            = cClothing;
            vm.ConsumerSpendingPinMoneyYear                   = cPin;
            vm.ConsumerSpendingProtectionTypeInsuranceYear    = cInsurance;
            vm.ConsumerSpendingOtherExpensesYear              = cOther;
            vm.ConsumerSpendingUnknownExpenditureYear         = cUnknown;
            if (totalConsumer != 0) { vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear = Pct(cMeal, totalConsumer); vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYear = Pct(cHousing, totalConsumer); vm.PercentageOfConsumerSpendingEducationExpensesYear = Pct(cEducation, totalConsumer); vm.PercentageOfConsumerSpendingMedicalExpensesYear = Pct(cMedical, totalConsumer); vm.PercentageOfConsumerSpendingTransportationCostYear = Pct(cTransport, totalConsumer); vm.PercentageOfConsumerSpendingCommunicationCostYear = Pct(cComm, totalConsumer); vm.PercentageOfConsumerSpendingLeisureOrCultureYear = Pct(cLeisure, totalConsumer); vm.PercentageOfConsumerSpendingClothingOrShoesYear = Pct(cClothing, totalConsumer); vm.PercentageOfConsumerSpendingPinMoneyYear = Pct(cPin, totalConsumer); vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYear = Pct(cInsurance, totalConsumer); vm.PercentageOfConsumerSpendingOtherExpensesYear = Pct(cOther, totalConsumer); vm.PercentageOfConsumerSpendingUnknownExpenditureYear = Pct(cUnknown, totalConsumer); }
        }
        else
        {
            vm.RegularSavingsDepositYearMonth    = savingsDeposit;
            vm.RegularSavingsInvestmentYearMonth = savingsInvestment;
            if (totalSavings != 0) { vm.PercentageOfRegularSavingsDepositYearMonth = Pct(savingsDeposit, totalSavings); vm.PercentageOfRegularSavingsInvestmentYearMonth = Pct(savingsInvestment, totalSavings); }

            vm.NonConsumerSpendingPublicPensionYearMonth                    = ncPublicPension;
            vm.NonConsumerSpendingDebtRepaymentYearMonth                    = ncDebt;
            vm.NonConsumerSpendingTaxYearMonth                              = ncTax;
            vm.NonConsumerSpendingSocialInsuranceYearMonth                  = ncSocial;
            vm.NonConsumerSpendingInterHouseholdTransferExpensesYearMonth   = ncInterHousehold;
            vm.NonConsumerSpendingNonProfitOrganizationTransferYearMonth    = ncNonProfit;
            if (totalNonConsumer != 0) { vm.PercentageOfNonConsumerSpendingPublicPensionYearMonth = Pct(ncPublicPension, totalNonConsumer); vm.PercentageOfNonConsumerSpendingDebtRepaymentYearMonth = Pct(ncDebt, totalNonConsumer); vm.PercentageOfNonConsumerSpendingTaxYearMonth = Pct(ncTax, totalNonConsumer); vm.PercentageOfNonConsumerSpendingSocialInsuranceYearMonth = Pct(ncSocial, totalNonConsumer); vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYearMonth = Pct(ncInterHousehold, totalNonConsumer); vm.PercentageOfNonConsumerSpendingNonProfitOrganizationTransferYearMonth = Pct(ncNonProfit, totalNonConsumer); }

            vm.ConsumerSpendingMealOrEatOutExpensesYearMonth       = cMeal;
            vm.ConsumerSpendingHousingOrSuppliesCostYearMonth      = cHousing;
            vm.ConsumerSpendingEducationExpensesYearMonth          = cEducation;
            vm.ConsumerSpendingMedicalExpensesYearMonth            = cMedical;
            vm.ConsumerSpendingTransportationCostYearMonth         = cTransport;
            vm.ConsumerSpendingCommunicationCostYearMonth          = cComm;
            vm.ConsumerSpendingLeisureOrCultureYearMonth           = cLeisure;
            vm.ConsumerSpendingClothingOrShoesYearMonth            = cClothing;
            vm.ConsumerSpendingPinMoneyYearMonth                   = cPin;
            vm.ConsumerSpendingProtectionTypeInsuranceYearMonth    = cInsurance;
            vm.ConsumerSpendingOtherExpensesYearMonth              = cOther;
            vm.ConsumerSpendingUnknownExpenditureYearMonth         = cUnknown;
            if (totalConsumer != 0) { vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYearMonth = Pct(cMeal, totalConsumer); vm.PercentageOfConsumerSpendingHousingOrSuppliesCostYearMonth = Pct(cHousing, totalConsumer); vm.PercentageOfConsumerSpendingEducationExpensesYearMonth = Pct(cEducation, totalConsumer); vm.PercentageOfConsumerSpendingMedicalExpensesYearMonth = Pct(cMedical, totalConsumer); vm.PercentageOfConsumerSpendingTransportationCostYearMonth = Pct(cTransport, totalConsumer); vm.PercentageOfConsumerSpendingLeisureOrCultureYearMonth = Pct(cLeisure, totalConsumer); vm.PercentageOfConsumerSpendingClothingOrShoesYearMonth = Pct(cClothing, totalConsumer); vm.PercentageOfConsumerSpendingPinMoneyYearMonth = Pct(cPin, totalConsumer); vm.PercentageOfConsumerSpendingProtectionTypeInsuranceYearMonth = Pct(cInsurance, totalConsumer); vm.PercentageOfConsumerSpendingOtherExpensesYearMonth = Pct(cOther, totalConsumer); vm.PercentageOfConsumerSpendingUnknownExpenditureYearMonth = Pct(cUnknown, totalConsumer); }
        }
    }

    private static decimal SumSub(IEnumerable<IncomeOutputViewModel> items, string subClass)
        => items.Where(x => x.SubClass == subClass).Sum(v => v.Amount);

    private static decimal SumSub(IEnumerable<ExpenditureOutputViewModel> items, string subClass)
        => items.Where(x => x.SubClass == subClass).Sum(v => v.Amount);

    private static double Pct(decimal part, decimal total) => 100.0 * (double)part / (double)total;
}
