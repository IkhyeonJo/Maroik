$(function () {
    //-------------
    //- DONUT CHART -
    //-------------
    // Get context with jQuery - using jQuery's .get() method.

    var regularIncomeLaborIncome = parseInt($("#regularIncomeLaborIncome").val());
    var regularIncomeBusinessIncome = parseInt($("#regularIncomeBusinessIncome").val());
    var regularIncomePensionIncome = parseInt($("#regularIncomePensionIncome").val());
    var regularIncomeFinancialIncome = parseInt($("#regularIncomeFinancialIncome").val());
    var regularIncomeRentalIncome = parseInt($("#regularIncomeRentalIncome").val());
    var regularIncomeOtherIncome = parseInt($("#regularIncomeOtherIncome").val());

    var irregularIncomeLaborIncome = $("#irregularIncomeLaborIncome").val();
    var irregularIncomeOtherIncome = $("#irregularIncomeOtherIncome").val();

    var regularIncomeDonutChartOptions = {
        series: [regularIncomeLaborIncome, regularIncomeBusinessIncome, regularIncomePensionIncome, regularIncomeFinancialIncome, regularIncomeRentalIncome, regularIncomeOtherIncome],
        chart: {
            width: 380,
            type: 'donut',
            dropShadow: {
                enabled: true,
                color: '#111',
                top: -1,
                left: 3,
                blur: 3,
                opacity: 0.2
            }
        },
        stroke: {
            width: 0,
        },
        plotOptions: {
            pie: {
                donut: {
                    labels: {
                        show: true,
                        total: {
                            showAlways: true,
                            show: true
                        }
                    }
                }
            }
        },

        labels: ["LaborIncome", "BusinessIncome", "PensionIncome", "FinancialIncome", "RentalIncome", "OtherIncome"],
        legend: {
            labels: {
                colors: 'white',
            }
        },
        markers: {
            colors: ['#F44336', '#E91E63', '#9C27B0']
        },
        dataLabels: {
            dataLabels: {
                style: {
                    colors: ['#F44336', '#E91E63', '#9C27B0']
                }
            },
            dropShadow: {
                blur: 3,
                opacity: 0.8
            }
        },
        states: {
            hover: {
                filter: 'none'
            }
        },
        theme: {
            palette: 'palette2'
        },
        title: {
            text: "Favourite Movie Type"
        },
        responsive: [{
            breakpoint: 480,
            options: {
                chart: {
                    width: 200
                },
                legend: {
                    position: 'bottom'
                }
            }
        }]
    };

    var chart = new ApexCharts(document.querySelector("#regularIncomeDonutChart"), regularIncomeDonutChartOptions);
    chart.render();
})