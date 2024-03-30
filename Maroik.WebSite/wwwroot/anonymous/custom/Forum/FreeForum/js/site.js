// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function SearchBoard() {
    window.location.href = "/Forum/FreeForum?searchType=" + $('#searchType').val() + "&searchText=" + $('#btnFreeForumSearchText').val();
}

$('#btnFreeForumSearchBoard').off('click').on('click', function () {
    SearchBoard();
});

$('#btnFreeForumWrite').off('click').on('click', function () {
    location.href = "/Forum/FreeForum?method=write";
});

$('#btnFreeForumList').off('click').on('click', function () {
    location.href = $(this).attr("data-link");
});

$('#btnFreeForumSearchText').off('keypress').on('keypress', function (event) {
    if (event.keyCode == 13 || event.which == 13) {
        SearchBoard();
    }
});

$('#formWriteFreeComment').off('keydown').on('keydown', function (event) {
    return event.key != 'Enter';
});