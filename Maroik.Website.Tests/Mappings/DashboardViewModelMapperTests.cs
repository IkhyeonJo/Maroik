using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Website.Mappings;

namespace Maroik.Website.Tests.Mappings;

/// <summary>
/// Unit tests for <see cref="DashboardViewModelMapper"/> — the per-category income/expenditure
/// aggregation and percentage-breakdown calculations that feed the Dashboard charts.
/// </summary>
public class DashboardViewModelMapperTests
{
    private const string Utc = "UTC";

    private static IncomeResponse Income(string mainClass, string subClass, decimal amount, string? asset = "Bank") => new()
    {
        Id = 1,
        MainClass = mainClass,
        SubClass = subClass,
        Amount = amount,
        DepositMyAssetProductName = asset,
        Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Updated = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
    };

    private static ExpenditureResponse Expenditure(string mainClass, string subClass, decimal amount, string? paymentMethod = "Card") => new()
    {
        Id = 1,
        MainClass = mainClass,
        SubClass = subClass,
        Amount = amount,
        PaymentMethod = paymentMethod,
        Created = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Updated = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
    };

    private static DashboardDto BaseDto() => new()
    {
        DefaultMonetaryUnit = "KRW",
        Assets = [],
        MonetaryUnits = ["KRW", null, "USD"],
        StartYear = 2020,
        EndYear = 2025,
        SelectedYear = 2025,
        SelectedMonth = 6
    };

    // -- Early-exit / basic field mapping -------------------------------------

    [Fact]
    public void ToViewModel_DefaultMonetaryUnitNull_ReturnsBasicFieldsOnlyWithNoAggregation()
    {
        var dto = BaseDto();
        dto.DefaultMonetaryUnit = null;
        dto.YearIncomes = [Income("RegularIncome", "LaborIncome", 100)];

        var vm = dto.ToViewModel(Utc);

        Assert.Empty(vm.IncomeYearOutputViewModels);
        Assert.Equal(0m, vm.RegularIncomeLaborIncomeYear);
    }

    [Fact]
    public void ToViewModel_CopiesYearSelectorsAndDefaultMonetaryUnit()
    {
        var dto = BaseDto();

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(2020, vm.StartYear);
        Assert.Equal(2025, vm.EndYear);
        Assert.Equal(2025, vm.SelectedYear);
        Assert.Equal(6, vm.SelectedMonth);
        Assert.Equal("KRW", vm.DefaultMonetaryUnit);
    }

    [Fact]
    public void ToViewModel_FiltersNullMonetaryUnits()
    {
        var dto = BaseDto();

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(["KRW", "USD"], vm.MonetaryUnits);
    }

    // -- Currency lookup + timezone conversion --------------------------------

    [Fact]
    public void ToViewModel_MapsIncomeMonetaryUnit_FromAssetLookupByDepositProductName()
    {
        var dto = BaseDto();
        dto.Assets = [new AssetResponse { ProductName = "Bank", MonetaryUnit = "USD" }];
        dto.YearIncomes = [Income("RegularIncome", "LaborIncome", 100, "Bank")];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal("USD", vm.IncomeYearOutputViewModels[0].MonetaryUnit);
    }

    [Fact]
    public void ToViewModel_MapsExpenditureMonetaryUnit_FromAssetLookupByPaymentMethod()
    {
        var dto = BaseDto();
        dto.Assets = [new AssetResponse { ProductName = "Card", MonetaryUnit = "USD" }];
        dto.YearExpenditures = [Expenditure("ConsumerSpending", "MealOrEatOutExpenses", 50, "Card")];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal("USD", vm.ExpenditureYearOutputViewModels[0].MonetaryUnit);
    }

    [Fact]
    public void ToViewModel_ConvertsIncomeCreatedAndUpdated_ToTargetTimeZone()
    {
        var dto = BaseDto();
        var income = Income("RegularIncome", "LaborIncome", 100);
        dto.YearIncomes = [income];

        var vm = dto.ToViewModel("Asia/Seoul");

        Assert.Equal(income.Created.ConvertTimeByTimeZoneIanaId("Asia/Seoul"), vm.IncomeYearOutputViewModels[0].Created);
        Assert.Equal(income.Updated.ConvertTimeByTimeZoneIanaId("Asia/Seoul"), vm.IncomeYearOutputViewModels[0].Updated);
    }

    // -- Income aggregation: RegularIncome / IrregularIncome ------------------

    [Fact]
    public void ToViewModel_AggregatesRegularIncomeSubClassesAndPercentages_Year()
    {
        var dto = BaseDto();
        dto.YearIncomes =
        [
            Income("RegularIncome", "LaborIncome", 600),
            Income("RegularIncome", "BusinessIncome", 300),
            Income("RegularIncome", "PensionIncome", 100),
            Income("IrregularIncome", "LaborIncome", 999) // must not bleed into RegularIncome totals
        ];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(600m, vm.RegularIncomeLaborIncomeYear);
        Assert.Equal(300m, vm.RegularIncomeBusinessIncomeYear);
        Assert.Equal(100m, vm.RegularIncomePensionIncomeYear);
        Assert.Equal(0m, vm.RegularIncomeFinancialIncomeYear);
        Assert.Equal(60.0, vm.PercentageOfRegularIncomeLaborIncomeYear);
        Assert.Equal(30.0, vm.PercentageOfRegularIncomeBusinessIncomeYear);
        Assert.Equal(10.0, vm.PercentageOfRegularIncomePensionIncomeYear);
    }

    [Fact]
    public void ToViewModel_AggregatesIrregularIncomeSubClassesAndPercentages_Year()
    {
        var dto = BaseDto();
        dto.YearIncomes =
        [
            Income("IrregularIncome", "LaborIncome", 750),
            Income("IrregularIncome", "OtherIncome", 250)
        ];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(750m, vm.IrregularIncomeLaborIncomeYear);
        Assert.Equal(250m, vm.IrregularIncomeOtherIncomeYear);
        Assert.Equal(75.0, vm.PercentageOfIrregularIncomeLaborIncomeYear);
        Assert.Equal(25.0, vm.PercentageOfIrregularIncomeOtherIncomeYear);
    }

    [Fact]
    public void ToViewModel_RegularIncomeTotalZero_PercentagesStayAtDefault()
    {
        var dto = BaseDto();
        dto.YearIncomes = []; // no regular income at all -> totalRegular == 0

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(0.0, vm.PercentageOfRegularIncomeLaborIncomeYear);
        Assert.Equal(0.0, vm.PercentageOfRegularIncomeBusinessIncomeYear);
    }

    [Fact]
    public void ToViewModel_AggregatesRegularIncome_ForYearMonthList_Independently()
    {
        var dto = BaseDto();
        dto.YearIncomes = [Income("RegularIncome", "LaborIncome", 1000)];
        dto.YearMonthIncomes = [Income("RegularIncome", "LaborIncome", 400), Income("RegularIncome", "BusinessIncome", 100)];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(1000m, vm.RegularIncomeLaborIncomeYear);
        Assert.Equal(400m, vm.RegularIncomeLaborIncomeYearMonth);
        Assert.Equal(100m, vm.RegularIncomeBusinessIncomeYearMonth);
        Assert.Equal(80.0, vm.PercentageOfRegularIncomeLaborIncomeYearMonth);
    }

    // -- Expenditure aggregation: RegularSavings -------------------------------

    [Fact]
    public void ToViewModel_AggregatesRegularSavingsSubClassesAndPercentages_Year()
    {
        var dto = BaseDto();
        dto.YearExpenditures =
        [
            Expenditure("RegularSavings", "Deposit", 700),
            Expenditure("RegularSavings", "Investment", 300)
        ];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(700m, vm.RegularSavingsDepositYear);
        Assert.Equal(300m, vm.RegularSavingsInvestmentYear);
        Assert.Equal(70.0, vm.PercentageOfRegularSavingsDepositYear);
        Assert.Equal(30.0, vm.PercentageOfRegularSavingsInvestmentYear);
    }

    // -- Expenditure aggregation: NonConsumerSpending --------------------------

    [Fact]
    public void ToViewModel_AggregatesNonConsumerSpendingSubClassesAndPercentages_Year()
    {
        var dto = BaseDto();
        dto.YearExpenditures =
        [
            Expenditure("NonConsumerSpending", "PublicPension", 200),
            Expenditure("NonConsumerSpending", "DebtRepayment", 200),
            Expenditure("NonConsumerSpending", "Tax", 200),
            Expenditure("NonConsumerSpending", "SocialInsurance", 200),
            Expenditure("NonConsumerSpending", "InterHouseholdTranserExpenses", 100),
            Expenditure("NonConsumerSpending", "NonProfitOrganizationTransfer", 100)
        ];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(200m, vm.NonConsumerSpendingPublicPensionYear);
        Assert.Equal(200m, vm.NonConsumerSpendingDebtRepaymentYear);
        Assert.Equal(200m, vm.NonConsumerSpendingTaxYear);
        Assert.Equal(200m, vm.NonConsumerSpendingSocialInsuranceYear);
        Assert.Equal(100m, vm.NonConsumerSpendingInterHouseholdTransferExpensesYear);
        Assert.Equal(100m, vm.NonConsumerSpendingNonProfitOrganizationTransferYear);
        Assert.Equal(20.0, vm.PercentageOfNonConsumerSpendingPublicPensionYear);
        Assert.Equal(10.0, vm.PercentageOfNonConsumerSpendingInterHouseholdTransferExpensesYear);
    }

    // -- Expenditure aggregation: ConsumerSpending (all 12 sub-classes) --------

    [Fact]
    public void ToViewModel_AggregatesConsumerSpendingSubClassesAndPercentages_Year()
    {
        var dto = BaseDto();
        dto.YearExpenditures =
        [
            Expenditure("ConsumerSpending", "MealOrEatOutExpenses", 100),
            Expenditure("ConsumerSpending", "HousingOrSuppliesCost", 100),
            Expenditure("ConsumerSpending", "EducationExpenses", 100),
            Expenditure("ConsumerSpending", "MedicalExpenses", 100),
            Expenditure("ConsumerSpending", "TransportationCost", 100),
            Expenditure("ConsumerSpending", "CommunicationCost", 100),
            Expenditure("ConsumerSpending", "LeisureOrCulture", 100),
            Expenditure("ConsumerSpending", "ClothingOrShoes", 100),
            Expenditure("ConsumerSpending", "PinMoney", 100),
            Expenditure("ConsumerSpending", "ProtectionTypeInsurance", 100),
            Expenditure("ConsumerSpending", "OtherExpenses", 100),
            Expenditure("ConsumerSpending", "UnknownExpenditure", 100)
        ];

        var vm = dto.ToViewModel(Utc);

        // 12 equal sub-classes -> each is exactly 1/12 of the total
        Assert.Equal(100m, vm.ConsumerSpendingMealOrEatOutExpensesYear);
        Assert.Equal(100m, vm.ConsumerSpendingUnknownExpenditureYear);
        Assert.Equal(100.0 / 12.0, vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear, precision: 10);
        Assert.Equal(100.0 / 12.0, vm.PercentageOfConsumerSpendingUnknownExpenditureYear, precision: 10);
    }

    [Fact]
    public void ToViewModel_ExpenditureTotalsZero_PercentagesStayAtDefault()
    {
        var dto = BaseDto();
        dto.YearExpenditures = [];

        var vm = dto.ToViewModel(Utc);

        Assert.Equal(0.0, vm.PercentageOfRegularSavingsDepositYear);
        Assert.Equal(0.0, vm.PercentageOfConsumerSpendingMealOrEatOutExpensesYear);
    }
}
