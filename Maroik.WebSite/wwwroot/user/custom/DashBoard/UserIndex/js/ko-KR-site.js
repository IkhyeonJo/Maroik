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
    let regularIncomeLaborIncome = Number($("#regularIncomeLaborIncome").val());
    let regularIncomeBusinessIncome = Number($("#regularIncomeBusinessIncome").val());
    let regularIncomePensionIncome = Number($("#regularIncomePensionIncome").val());
    let regularIncomeFinancialIncome = Number($("#regularIncomeFinancialIncome").val());
    let regularIncomeRentalIncome = Number($("#regularIncomeRentalIncome").val());
    let regularIncomeOtherIncome = Number($("#regularIncomeOtherIncome").val());

    let donutChartCanvasRegularIncome = $('#regularIncome').get(0).getContext('2d')
    let donutDataRegularIncome = {
        labels: [
            '근로수입',
            '사업수입',
            '연금수입',
            '금융소득',
            '임대수입',
            '기타수입',
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



    let irregularIncomeLaborIncome = Number($("#irregularIncomeLaborIncome").val());
    let irregularIncomeOtherIncome = Number($("#irregularIncomeOtherIncome").val());

    let donutChartCanvasIrregularIncome = $('#irregularIncome').get(0).getContext('2d')
    let donutDataIrregularIncome = {
        labels: [
            '근로수입',
            '기타수입',
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



    let regularSavingsDeposit = Number($("#regularSavingsDeposit").val());
    let regularSavingsInvestment = Number($("#regularSavingsInvestment").val());

    let donutChartCanvasRegularSavings = $('#regularSavings').get(0).getContext('2d')
    let donutDataRegularSavings = {
        labels: [
            '예적금',
            '투자',
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



    let nonConsumerSpendingPublicPension = Number($("#nonConsumerSpendingPublicPension").val());
    let nonConsumerSpendingDebtRepayment = Number($("#nonConsumerSpendingDebtRepayment").val());
    let nonConsumerSpendingTax = Number($("#nonConsumerSpendingTax").val());
    let nonConsumerSpendingSocialInsurance = Number($("#nonConsumerSpendingSocialInsurance").val());
    let nonConsumerSpendingInterHouseholdTranserExpenses = Number($("#nonConsumerSpendingInterHouseholdTranserExpenses").val());
    let nonConsumerSpendingNonProfitOrganizationTransfer = Number($("#nonConsumerSpendingNonProfitOrganizationTransfer").val());

    let donutChartCanvasNonConsumerSpending = $('#nonConsumerSpending').get(0).getContext('2d')
    let donutDataNonConsumerSpending = {
        labels: [
            '공적연금',
            '부채상환',
            '세금',
            '사회보험',
            '가구간 이전지출',
            '비영리단체 이전',
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
            '식비/외식비',
            '주거/용품비',
            '교육비',
            '의료비',
            '교통비',
            '통신비',
            '여가/문화',
            '의류/신발',
            '용돈',
            '보장성보험',
            '기타지출',
            '미파악지출',
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