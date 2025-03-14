const localizer = {
    LaborIncome: $('#localizerLaborIncome').val(),
    BusinessIncome: $('#localizerBusinessIncome').val(),
    PensionIncome: $('#localizerPensionIncome').val(),
    FinancialIncome: $('#localizerFinancialIncome').val(),
    RentalIncome: $('#localizerRentalIncome').val(),
    OtherIncome: $('#localizerOtherIncome').val(),
    Deposit: $('#localizerDeposit').val(),
    Investment: $('#localizerInvestment').val(),
    PublicPension: $('#localizerPublicPension').val(),
    DebtRepayment: $('#localizerDebtRepayment').val(),
    Tax: $('#localizerTax').val(),
    SocialInsurance: $('#localizerSocialInsurance').val(),
    InterHouseholdTransferExpenses: $('#localizerInterHouseholdTransferExpenses').val(),
    NonProfitOrganizationTransfer: $('#localizerNonProfitOrganizationTransfer').val(),
    MealOrEatOutExpenses: $('#localizerMealOrEatOutExpenses').val(),
    HousingOrSuppliesCost: $('#localizerHousingOrSuppliesCost').val(),
    EducationExpenses: $('#localizerEducationExpenses').val(),
    MedicalExpenses: $('#localizerMedicalExpenses').val(),
    TransportationCost: $('#localizerTransportationCost').val(),
    CommunicationCost: $('#localizerCommunicationCost').val(),
    LeisureOrCulture: $('#localizerLeisureOrCulture').val(),
    ClothingOrShoes: $('#localizerClothingOrShoes').val(),
    PinMoney: $('#localizerPinMoney').val(),
    ProtectionTypeInsurance: $('#localizerProtectionTypeInsurance').val(),
    OtherExpenses: $('#localizerOtherExpenses').val(),
    UnknownExpenditure: $('#localizerUnknownExpenditure').val()
};

$('#year').off('change').on('change', function () {
    window.location.href = "/DashBoard/UserIndex?year=" + $('#year').find(":selected").val() + "&month=" + $('#month').find(":selected").val();
});

$('#month').off('change').on('change', function () {
    window.location.href = "/DashBoard/UserIndex?year=" + $('#year').find(":selected").val() + "&month=" + $('#month').find(":selected").val();
});

$('#monetaryUnit').off('change').on('change', function () {
    let paramValue = JSON.stringify({
        DefaultMonetaryUnit: $('#monetaryUnit').find(":selected").val()
    });

    $.ajax({
        url: '/DashBoard/UserUpdateDefaultMonetary',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                window.location.href = "/DashBoard/UserIndex?year=" + $('#year').find(":selected").val() + "&month=" + $('#month').find(":selected").val();
            }
            else {
                window.location.href = "/DashBoard/UserIndex?year=" + $('#year').find(":selected").val() + "&month=" + $('#month').find(":selected").val();
            }
        }
    });
});

$(function () {
    let regularIncomeLaborIncome = Number($("#regularIncomeLaborIncome").val());
    let regularIncomeBusinessIncome = Number($("#regularIncomeBusinessIncome").val());
    let regularIncomePensionIncome = Number($("#regularIncomePensionIncome").val());
    let regularIncomeFinancialIncome = Number($("#regularIncomeFinancialIncome").val());
    let regularIncomeRentalIncome = Number($("#regularIncomeRentalIncome").val());
    let regularIncomeOtherIncome = Number($("#regularIncomeOtherIncome").val());

    let donutChartCanvasRegularIncome = $('#regularIncome').get(0).getContext('2d')
    let donutDataRegularIncome = {
        labels: [
            localizer.LaborIncome,
            localizer.BusinessIncome,
            localizer.PensionIncome,
            localizer.FinancialIncome,
            localizer.RentalIncome,
            localizer.OtherIncome,
        ],
        datasets: [
            {
                data: [regularIncomeLaborIncome, regularIncomeBusinessIncome, regularIncomePensionIncome, regularIncomeFinancialIncome, regularIncomeRentalIncome, regularIncomeOtherIncome],
                backgroundColor: ['#f56954', '#00a65a', '#f39c12', '#00c0ef', '#3c8dbc', '#d2d6de'],
            }
        ]
    }
    let donutOptionsRegularIncome = {
        maintainAspectRatio: false,
        responsive: true,
        legend: {
            labels: {
                fontColor: '#fff',
                fontSize: 15
            }
        }
    }

    new Chart(donutChartCanvasRegularIncome, {
        type: 'doughnut',
        data: donutDataRegularIncome,
        options: donutOptionsRegularIncome
    })



    let irregularIncomeLaborIncome = Number($("#irregularIncomeLaborIncome").val());
    let irregularIncomeOtherIncome = Number($("#irregularIncomeOtherIncome").val());

    let donutChartCanvasIrregularIncome = $('#irregularIncome').get(0).getContext('2d')
    let donutDataIrregularIncome = {
        labels: [
            localizer.LaborIncome,
            localizer.OtherIncome,
        ],
        datasets: [
            {
                data: [irregularIncomeLaborIncome, irregularIncomeOtherIncome],
                backgroundColor: ['#f56954', '#00a65a'],
            }
        ]
    }
    let donutOptionsIrregularIncome = {
        maintainAspectRatio: false,
        responsive: true,
        legend: {
            labels: {
                fontColor: '#fff',
                fontSize: 15
            }
        }
    }

    new Chart(donutChartCanvasIrregularIncome, {
        type: 'doughnut',
        data: donutDataIrregularIncome,
        options: donutOptionsIrregularIncome
    })



    let regularSavingsDeposit = Number($("#regularSavingsDeposit").val());
    let regularSavingsInvestment = Number($("#regularSavingsInvestment").val());

    let donutChartCanvasRegularSavings = $('#regularSavings').get(0).getContext('2d')
    let donutDataRegularSavings = {
        labels: [
            localizer.Deposit,
            localizer.Investment,
        ],
        datasets: [
            {
                data: [regularSavingsDeposit, regularSavingsInvestment],
                backgroundColor: ['#f56954', '#00a65a'],
            }
        ]
    }
    let donutOptionsRegularSavings = {
        maintainAspectRatio: false,
        responsive: true,
        legend: {
            labels: {
                fontColor: '#fff',
                fontSize: 15
            }
        }
    }

    new Chart(donutChartCanvasRegularSavings, {
        type: 'doughnut',
        data: donutDataRegularSavings,
        options: donutOptionsRegularSavings
    })



    let nonConsumerSpendingPublicPension = Number($("#nonConsumerSpendingPublicPension").val());
    let nonConsumerSpendingDebtRepayment = Number($("#nonConsumerSpendingDebtRepayment").val());
    let nonConsumerSpendingTax = Number($("#nonConsumerSpendingTax").val());
    let nonConsumerSpendingSocialInsurance = Number($("#nonConsumerSpendingSocialInsurance").val());
    let nonConsumerSpendingInterHouseholdTranserExpenses = Number($("#nonConsumerSpendingInterHouseholdTranserExpenses").val());
    let nonConsumerSpendingNonProfitOrganizationTransfer = Number($("#nonConsumerSpendingNonProfitOrganizationTransfer").val());

    let donutChartCanvasNonConsumerSpending = $('#nonConsumerSpending').get(0).getContext('2d')
    let donutDataNonConsumerSpending = {
        labels: [
            localizer.PublicPension,
            localizer.DebtRepayment,
            localizer.Tax,
            localizer.SocialInsurance,
            localizer.InterHouseholdTransferExpenses,
            localizer.NonProfitOrganizationTransfer,
        ],
        datasets: [
            {
                data: [nonConsumerSpendingPublicPension, nonConsumerSpendingDebtRepayment, nonConsumerSpendingTax, nonConsumerSpendingSocialInsurance, nonConsumerSpendingInterHouseholdTranserExpenses, nonConsumerSpendingNonProfitOrganizationTransfer],
                backgroundColor: ['#f56954', '#00a65a', '#f39c12', '#00c0ef', '#3c8dbc', '#d2d6de'],
            }
        ]
    }
    let donutOptionsNonConsumerSpending = {
        maintainAspectRatio: false,
        responsive: true,
        legend: {
            labels: {
                fontColor: '#fff',
                fontSize: 15
            }
        }
    }

    new Chart(donutChartCanvasNonConsumerSpending, {
        type: 'doughnut',
        data: donutDataNonConsumerSpending,
        options: donutOptionsNonConsumerSpending
    })



    let consumerSpendingMealOrEatOutExpenses = Number($("#consumerSpendingMealOrEatOutExpenses").val());
    let consumerSpendingHousingOrSuppliesCost = Number($("#consumerSpendingHousingOrSuppliesCost").val());
    let consumerSpendingEducationExpenses = Number($("#consumerSpendingEducationExpenses").val());
    let consumerSpendingMedicalExpenses = Number($("#consumerSpendingMedicalExpenses").val());
    let consumerSpendingTransportationCost = Number($("#consumerSpendingTransportationCost").val());
    let consumerSpendingCommunicationCost = Number($("#consumerSpendingCommunicationCost").val());
    let consumerSpendingLeisureOrCulture = Number($("#consumerSpendingLeisureOrCulture").val());
    let consumerSpendingClothingOrShoes = Number($("#consumerSpendingClothingOrShoes").val());
    let consumerSpendingPinMoney = Number($("#consumerSpendingPinMoney").val());
    let consumerSpendingProtectionTypeInsurance = Number($("#consumerSpendingProtectionTypeInsurance").val());
    let consumerSpendingOtherExpenses = Number($("#consumerSpendingOtherExpenses").val());
    let consumerSpendingUnknownExpenditure = Number($("#consumerSpendingUnknownExpenditure").val());

    let donutChartCanvasConsumerSpending = $('#consumerSpending').get(0).getContext('2d')
    let donutDataConsumerSpending = {
        labels: [
            localizer.MealOrEatOutExpenses,
            localizer.HousingOrSuppliesCost,
            localizer.EducationExpenses,
            localizer.MedicalExpenses,
            localizer.TransportationCost,
            localizer.CommunicationCost,
            localizer.LeisureOrCulture,
            localizer.ClothingOrShoes,
            localizer.PinMoney,
            localizer.ProtectionTypeInsurance,
            localizer.OtherExpenses,
            localizer.UnknownExpenditure,
        ],
        datasets: [
            {
                data: [consumerSpendingMealOrEatOutExpenses, consumerSpendingHousingOrSuppliesCost, consumerSpendingEducationExpenses, consumerSpendingMedicalExpenses, consumerSpendingTransportationCost, consumerSpendingCommunicationCost, consumerSpendingLeisureOrCulture, consumerSpendingClothingOrShoes, consumerSpendingPinMoney, consumerSpendingProtectionTypeInsurance, consumerSpendingOtherExpenses, consumerSpendingUnknownExpenditure],
                backgroundColor: ['#FF0000', '#FFA200', '#FBFF00', '#70FF00', '#00FFE0', '#003EFF', '#D500FF', '#813F3F', '#7D813F', '#3F815A', '#3F8180', '#3F4B81'],
            }
        ]
    }
    let donutOptionsConsumerSpending = {
        maintainAspectRatio: false,
        responsive: true,
        legend: {
            labels: {
                fontColor: '#fff',
                fontSize: 15
            }
        }
    }

    new Chart(donutChartCanvasConsumerSpending, {
        type: 'doughnut',
        data: donutDataConsumerSpending,
        options: donutOptionsConsumerSpending
    })
})