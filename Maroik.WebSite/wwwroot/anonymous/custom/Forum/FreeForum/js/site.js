// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function SearchBoard() {
    window.location.href = "/Forum/FreeForum?searchType=" + $('#searchType').val() + "&searchText=" + $('#searchText').val();
}