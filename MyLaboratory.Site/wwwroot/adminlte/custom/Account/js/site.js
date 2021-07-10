// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    //$('#loading').hide();
});
function ShowLoading() {
    if (!$('#registerForm').valid()) {
        $('#loading').hide();
        return false;
    }
    else {
        $('#loading').show();
        return true;
    }
}