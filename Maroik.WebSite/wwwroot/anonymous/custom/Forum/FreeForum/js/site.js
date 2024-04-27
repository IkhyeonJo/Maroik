// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {

    if ($('#free_forum_detail_updated').length > 0) {
        var utcDateStr = $('#free_forum_detail_updated').text();
        var utcDate = new Date(utcDateStr);
        var localDateStr = utcDate.toLocaleString();
        $('#free_forum_detail_updated').text(localDateStr);
    }

    $('#tblFreeForum tbody tr .free_forum_board_created').each(function () {
        var $element = $(this);
        var $span = $element.find('span');
        var utcDateStr = $span.length > 0 ? $span.html() : $element.text(); // span 태그가 있는 경우 span 태그의 HTML을, 없는 경우 그대로 텍스트를 가져옴
        var utcDate = new Date(utcDateStr);
        var localDateStr = utcDate.toLocaleString();
        $span.length > 0 ? $span.html(localDateStr) : $element.text(localDateStr); // span 태그가 있는 경우 span 태그 안에, 없는 경우 그대로 텍스트를 변환된 날짜로 업데이트
    });

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
})