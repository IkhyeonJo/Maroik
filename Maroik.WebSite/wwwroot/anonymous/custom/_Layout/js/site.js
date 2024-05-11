// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).on("ajaxError", function () {
    window.location.href = "/DashBoard/AnonymousIndex";
});

function ChangeCulture(culture) {

    let paramValue = JSON.stringify({
        Culture: culture
    });

    $.ajax({
        url: '/DashBoard/CultureManagement',
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
}

$('#aChangeCultureEnUS').off('click').on('click', function () {
    ChangeCulture('en-US');
});

$('#aChangeCultureKoKR').off('click').on('click', function () {
    ChangeCulture('ko-KR');
});

$('#btnMainTopBarLogin').off('click').on('click', function () {
    location.href = "/Account/Login";
});