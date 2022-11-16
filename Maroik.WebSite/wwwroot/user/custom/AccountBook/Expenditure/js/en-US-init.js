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

const currentYear = $("#createExpenditureDate").val().split('-')[0];
const currentMonth = $("#createExpenditureDate").val().split('-')[1];
const currentDay = $("#createExpenditureDate").val().split('-')[2];

$(function () {
    $("#createExpenditureTabs").tabs();
    $("#editExpenditureTabs").tabs();
    $("#createExpenditureDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {

            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {

            }, 1);
        }
    });

    $('<style type="text/css"> .ui-datepicker-close { display: none; } </style>').appendTo("head");
    $('<style type="text/css"> .ui-datepicker-current { display: none; } </style>').appendTo("head");
});