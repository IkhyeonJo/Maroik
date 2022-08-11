// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function ChangeCulture(culture) {
    
    var paramValue = JSON.stringify({
        Culture: culture
    });

    $.ajax({
        url: '/Account/CultureManagement',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                window.location.href = "/DashBoard/AnonymousIndex/";
            }
            else {
                window.location.href = "/DashBoard/AnonymousIndex/";
            }
        }
    });
}