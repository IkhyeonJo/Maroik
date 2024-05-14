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
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                window.location.href = $("#returnUri").val();
            }
            else {
                window.location.href = $("#returnUri").val();
            }
        }
    });
});

$(function () {
    let regularIncomeLaborIncome = parseInt($("#regularIncomeLaborIncome").val());
    let regularIncomeBusinessIncome = parseInt($("#regularIncomeBusinessIncome").val());
    let regularIncomePensionIncome = parseInt($("#regularIncomePensionIncome").val());
    let regularIncomeFinancialIncome = parseInt($("#regularIncomeFinancialIncome").val());
    let regularIncomeRentalIncome = parseInt($("#regularIncomeRentalIncome").val());
    let regularIncomeOtherIncome = parseInt($("#regularIncomeOtherIncome").val());

    let donutChartCanvasRegularIncome = $('#regularIncome').get(0).getContext('2d')
    let donutDataRegularIncome = {
        labels: [
            'LaborIncome',
            'BusinessIncome',
            'PensionIncome',
            'FinancialIncome',
            'RentalIncome',
            'OtherIncome',
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
    }

    new Chart(donutChartCanvasRegularIncome, {
        type: 'doughnut',
        data: donutDataRegularIncome,
        options: donutOptionsRegularIncome
    })



    let irregularIncomeLaborIncome = parseInt($("#irregularIncomeLaborIncome").val());
    let irregularIncomeOtherIncome = parseInt($("#irregularIncomeOtherIncome").val());

    let donutChartCanvasIrregularIncome = $('#irregularIncome').get(0).getContext('2d')
    let donutDataIrregularIncome = {
        labels: [
            'LaborIncome',
            'OtherIncome',
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
    }

    new Chart(donutChartCanvasIrregularIncome, {
        type: 'doughnut',
        data: donutDataIrregularIncome,
        options: donutOptionsIrregularIncome
    })



    let regularSavingsDeposit = parseInt($("#regularSavingsDeposit").val());
    let regularSavingsInvestment = parseInt($("#regularSavingsInvestment").val());

    let donutChartCanvasRegularSavings = $('#regularSavings').get(0).getContext('2d')
    let donutDataRegularSavings = {
        labels: [
            'Deposit',
            'Investment',
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
    }

    new Chart(donutChartCanvasRegularSavings, {
        type: 'doughnut',
        data: donutDataRegularSavings,
        options: donutOptionsRegularSavings
    })



    let nonConsumerSpendingPublicPension = parseInt($("#nonConsumerSpendingPublicPension").val());
    let nonConsumerSpendingDebtRepayment = parseInt($("#nonConsumerSpendingDebtRepayment").val());
    let nonConsumerSpendingTax = parseInt($("#nonConsumerSpendingTax").val());
    let nonConsumerSpendingSocialInsurance = parseInt($("#nonConsumerSpendingSocialInsurance").val());
    let nonConsumerSpendingInterHouseholdTranserExpenses = parseInt($("#nonConsumerSpendingInterHouseholdTranserExpenses").val());
    let nonConsumerSpendingNonProfitOrganizationTransfer = parseInt($("#nonConsumerSpendingNonProfitOrganizationTransfer").val());

    let donutChartCanvasNonConsumerSpending = $('#nonConsumerSpending').get(0).getContext('2d')
    let donutDataNonConsumerSpending = {
        labels: [
            'PublicPension',
            'DebtRepayment',
            'Tax',
            'SocialInsurance',
            'InterHouseholdTransferExpenses',
            'NonProfitOrganizationTransfer',
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
    }

    new Chart(donutChartCanvasNonConsumerSpending, {
        type: 'doughnut',
        data: donutDataNonConsumerSpending,
        options: donutOptionsNonConsumerSpending
    })



    let consumerSpendingMealOrEatOutExpenses = parseInt($("#consumerSpendingMealOrEatOutExpenses").val());
    let consumerSpendingHousingOrSuppliesCost = parseInt($("#consumerSpendingHousingOrSuppliesCost").val());
    let consumerSpendingEducationExpenses = parseInt($("#consumerSpendingEducationExpenses").val());
    let consumerSpendingMedicalExpenses = parseInt($("#consumerSpendingMedicalExpenses").val());
    let consumerSpendingTransportationCost = parseInt($("#consumerSpendingTransportationCost").val());
    let consumerSpendingCommunicationCost = parseInt($("#consumerSpendingCommunicationCost").val());
    let consumerSpendingLeisureOrCulture = parseInt($("#consumerSpendingLeisureOrCulture").val());
    let consumerSpendingClothingOrShoes = parseInt($("#consumerSpendingClothingOrShoes").val());
    let consumerSpendingPinMoney = parseInt($("#consumerSpendingPinMoney").val());
    let consumerSpendingProtectionTypeInsurance = parseInt($("#consumerSpendingProtectionTypeInsurance").val());
    let consumerSpendingOtherExpenses = parseInt($("#consumerSpendingOtherExpenses").val());
    let consumerSpendingUnknownExpenditure = parseInt($("#consumerSpendingUnknownExpenditure").val());

    let donutChartCanvasConsumerSpending = $('#consumerSpending').get(0).getContext('2d')
    let donutDataConsumerSpending = {
        labels: [
            'MealOrEatOutExpenses',
            'HousingOrSuppliesCost',
            'EducationExpenses',
            'MedicalExpenses',
            'TransportationCost',
            'CommunicationCost',
            'LeisureOrCulture',
            'ClothingOrShoes',
            'PinMoney',
            'ProtectionTypeInsurance',
            'OtherExpenses',
            'UnknownExpenditure',
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
    }

    new Chart(donutChartCanvasConsumerSpending, {
        type: 'doughnut',
        data: donutDataConsumerSpending,
        options: donutOptionsConsumerSpending
    })
})