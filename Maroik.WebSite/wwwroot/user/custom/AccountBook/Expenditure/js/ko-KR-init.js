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

    $("#editExpenditureDate").datepicker({
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