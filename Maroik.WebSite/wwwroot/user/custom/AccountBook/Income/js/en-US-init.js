// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$.datepicker.setDefaults({
    dateFormat: 'yy-mm-dd',
    prevText: 'Previous Month',
    nextText: 'Next Month',
    monthNames: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
    monthNamesShort: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
    dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
    dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
    dayNamesMin: ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'],
    showMonthAfterYear: true,
    yearSuffix: ''
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
                    text: "Today",
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
                    text: "Today",
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