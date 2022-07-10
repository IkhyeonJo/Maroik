$(function () {
    var regularIncomeLaborIncome = parseInt($("#regularIncomeLaborIncome").val());
    var regularIncomeBusinessIncome = parseInt($("#regularIncomeBusinessIncome").val());
    var regularIncomePensionIncome = parseInt($("#regularIncomePensionIncome").val());
    var regularIncomeFinancialIncome = parseInt($("#regularIncomeFinancialIncome").val());
    var regularIncomeRentalIncome = parseInt($("#regularIncomeRentalIncome").val());
    var regularIncomeOtherIncome = parseInt($("#regularIncomeOtherIncome").val());

    var donutChartCanvasRegularIncome = $('#regularIncome').get(0).getContext('2d')
    var donutDataRegularIncome = {
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
    var donutOptionsRegularIncome = {
        maintainAspectRatio: false,
        responsive: true,
    }
    //Create pie or douhnut chart
    // You can switch between pie and douhnut using the method below.
    new Chart(donutChartCanvasRegularIncome, {
        type: 'doughnut',
        data: donutDataRegularIncome,
        options: donutOptionsRegularIncome
    })



    var irregularIncomeLaborIncome = parseInt($("#irregularIncomeLaborIncome").val());
    var irregularIncomeOtherIncome = parseInt($("#irregularIncomeOtherIncome").val());

    var donutChartCanvasIrregularIncome = $('#irregularIncome').get(0).getContext('2d')
    var donutDataIrregularIncome = {
        labels: [
            'LaborIncome',
            'OtherIncome',
        ],
        datasets: [
            {
                data: [irregularIncomeLaborIncome, irregularIncomeOtherIncome],
                backgroundColor: ['#f56954', '#00a65a', '#f39c12', '#00c0ef', '#3c8dbc', '#d2d6de'],
            }
        ]
    }
    var donutOptionsIrregularIncome = {
        maintainAspectRatio: false,
        responsive: true,
    }
    //Create pie or douhnut chart
    // You can switch between pie and douhnut using the method below.
    new Chart(donutChartCanvasIrregularIncome, {
        type: 'doughnut',
        data: donutDataIrregularIncome,
        options: donutOptionsIrregularIncome
    })
})