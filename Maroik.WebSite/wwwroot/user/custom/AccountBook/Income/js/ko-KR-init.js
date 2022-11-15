// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$.datepicker.setDefaults({
    dateFormat: 'yy-mm-dd',
    prevText: '이전 달',
    nextText: '다음 달',
    monthNames: ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'],
    monthNamesShort: ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'],
    dayNames: ['일', '월', '화', '수', '목', '금', '토'],
    dayNamesShort: ['일', '월', '화', '수', '목', '금', '토'],
    dayNamesMin: ['일', '월', '화', '수', '목', '금', '토'],
    showMonthAfterYear: true,
    yearSuffix: '년'
});

const currentYear = $("#createIncomeDate").val().split('-')[0];
const currentMonth = $("#createIncomeDate").val().split('-')[1];
const currentDay = $("#createIncomeDate").val().split('-')[2];

$(function () {
    $("#createIncomeTabs").tabs();
    $("#editIncomeTabs").tabs();
    $("#createIncomeDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                var buttonPane = $(input)
                    .datepicker("widget")
                    .find(".ui-datepicker-buttonpane");

                $("<button>", {
                    text: "오늘",
                    click: function () {
                        $.datepicker._clearDate(input);
                        $("#createIncomeDate").datepicker("setDate", new Date(currentYear, currentMonth - 1, currentDay));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {
                var buttonPane = $(instance)
                    .datepicker("widget")
                    .find(".ui-datepicker-buttonpane");

                $("<button>", {
                    text: "오늘",
                    click: function () {
                        $.datepicker._clearDate(instance.input);
                        $("#createIncomeDate").datepicker("setDate", new Date(currentYear, currentMonth - 1, currentDay));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

            }, 1);
        }
    });

    $('<style type="text/css"> .ui-datepicker-close { display: none; } </style>').appendTo("head");
    $('<style type="text/css"> .ui-datepicker-current { display: none; } </style>').appendTo("head");
});