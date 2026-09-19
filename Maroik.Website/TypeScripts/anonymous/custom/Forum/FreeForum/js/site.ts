/**
 * Script for the **anonymous** view of the Free Forum
 * (`Views/Forum/FreeForum.cshtml` rendered for signed-out visitors — list,
 * search and read-only post detail).
 *
 * Responsibilities:
 *
 *   1. Convert every server-rendered UTC timestamp on the page to the visitor's
 *      local time (the server emits UTC; the browser knows the timezone).
 *   2. Wire the search box / buttons (navigations with query-string params).
 *   3. Rehydrate inline images in the post body and the attachment download
 *      link — both are shipped as base64 in `data-*` attributes and turned into
 *      object URLs here so nothing extra is fetched.
 *
 * IIFE-wrapped, no `import` / `export`. Most of the work runs inside a jQuery
 * `$(function)` ready callback.
 */
(function () {
    // Cached element references. `#free_forum_detail_updated` and the table only
    // exist on the relevant sub-view, so their `.length` is checked before use.
    const $freeForumDetailUpdated = $("#free_forum_detail_updated");
    const $tblFreeForum = $("#tblFreeForum");
    const $searchType = $("#searchType");
    const $btnFreeForumSearchText = $("#btnFreeForumSearchText");
    const $btnFreeForumSearchBoard = $("#btnFreeForumSearchBoard");
    const $btnFreeForumWrite = $("#btnFreeForumWrite");
    const $btnFreeForumList = $("#btnFreeForumList");
    const $formWriteFreeComment = $("#formWriteFreeComment");
    const $detailBoardContent = $("#detailBoardContent");
    const $aDetailBoardAttachedFile = $("#aDetailBoardAttachedFile");

    /**
     * Decodes a base64 string into a `Blob` of the given MIME type. Used to turn
     * the `data-file` payloads the server embeds into object URLs for `<img>`
     * sources and attachment downloads.
     *
     * @param base64 raw base64 (no `data:` prefix).
     * @param mime   MIME type for the resulting Blob.
     */
    function base64ToBlob(base64: string, mime: string) {
        const byteCharacters = atob(base64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        return new Blob([byteArray], { type: mime });
    }

    $(function () {

        // --- UTC -> local time -------------------------------------------------
        // Post-detail "updated" stamp (present only on the detail sub-view).
        if ($freeForumDetailUpdated.length > 0) {
            let utcDateStr = $freeForumDetailUpdated.text();
            let utcDate = new Date(utcDateStr);
            let localDateStr = utcDate.toLocaleString();
            $freeForumDetailUpdated.text(localDateStr);
        }

        // "Created" column of every row in the list table. The cell sometimes
        // wraps the value in a <span>, so read/write whichever is present.
        $tblFreeForum.find("tbody").children("tr").find(".free_forum_board_created").each(function () {
            let $element = $(this);
            let $span = $element.find("span");
            let utcDateStr = $span.length > 0 ? $span.html() : $element.text();
            let utcDate = new Date(utcDateStr);
            let localDateStr = utcDate.toLocaleString();
            $span.length > 0 ? $span.html(localDateStr) : $element.text(localDateStr);
        });

        // Each comment's "created" stamp on the detail view.
        $(".free_forum_detail_comment_created").each(function () {
            let utcDateStr = $(this).text();
            let utcDate = new Date(utcDateStr);
            let localDateStr = utcDate.toLocaleString();
            $(this).text(localDateStr);
        });

        // --- Search / navigation --------------------------------------------
        /** Reloads the list filtered by the chosen search type + text. */
        function SearchBoard() {
            window.location.href = "/Forum/FreeForum?searchType=" + encodeURIComponent($searchType.val() as string) + "&searchText=" + encodeURIComponent($btnFreeForumSearchText.val() as string);
        }

        $btnFreeForumSearchBoard.off("click").on("click", function () {
            SearchBoard();
        });

        $btnFreeForumWrite.off("click").on("click", function () {
            location.href = "/Forum/FreeForum?method=write";
        });

        // "Back to list" keeps the caller's paging/search state, carried in the
        // button's `data-link` attribute (always present here, hence the `!`).
        $btnFreeForumList.off("click").on("click", function () {
            location.href = $(this).attr("data-link")!;
        });

        // Enter in the search box triggers the search instead of submitting.
        $btnFreeForumSearchText.off("keypress").on("keypress", function (event) {
            if (event.keyCode === 13 || event.which === 13) {
                SearchBoard();
            }
        });

        // In the comment form, swallow Enter so a stray newline can't submit it
        // (returning false from a keydown handler cancels the key).
        $formWriteFreeComment.off("keydown").on("keydown", function (event) {
            return event.key !== "Enter";
        });

        // --- Inline images in the post body --------------------------------
        // The body is rendered hidden with its images as base64 `data-file`
        // attributes; swap each to an object URL, then reveal it. Parsing into a
        // detached document keeps the work off the live DOM until it is ready.
        if ($detailBoardContent.length > 0 && $detailBoardContent.is(":hidden")) {
            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString($detailBoardContent.html(), "text/html");

            const imgTags = htmlDoc.querySelectorAll<HTMLImageElement>("img[data-file]");

            imgTags.forEach(function (imgTag) {
                const base64Data = imgTag.getAttribute("data-file");
                const contentType = imgTag.getAttribute("data-contenttype");

                if (base64Data && contentType) {
                    const blob = base64ToBlob(base64Data, contentType);
                    const imgURL = URL.createObjectURL(blob);

                    imgTag.src = imgURL;

                    // Release the object URL once the browser has the bytes
                    // (whether the load succeeded or failed).
                    imgTag.onload = function () {
                        URL.revokeObjectURL(imgURL);
                    };
                    imgTag.onerror = function () {
                        URL.revokeObjectURL(imgURL);
                    };

                    // Drop the now-redundant payload attributes.
                    imgTag.removeAttribute("data-file");
                    imgTag.removeAttribute("data-contenttype");
                }
            });

            const updatedHtml = htmlDoc.body.innerHTML;
            $detailBoardContent.html(updatedHtml);
            $detailBoardContent.show();
        }

        // --- Attachment download -------------------------------------------
        // The attachment is also embedded as base64; build a Blob, hand it to a
        // throwaway <a download>, click it, then clean up.
        if ($aDetailBoardAttachedFile.length > 0) {
            $aDetailBoardAttachedFile.off("click").on("click", function (event) {
                event.preventDefault();
                let base64Data = $(this).attr("data-file");
                let contentType = $(this).attr("data-contenttype");
                let name = $(this).attr("data-name")

                if (base64Data && contentType) {
                    let blob = base64ToBlob(base64Data, contentType);
                    let url = URL.createObjectURL(blob);
                    let a = document.createElement("a");
                    try {
                        a.href = url;
                        a.download = name!;
                        a.click();
                    } finally {
                        // Revoke after a tick so the download has started; drop the <a>.
                        setTimeout(() => URL.revokeObjectURL(url), 100);
                        a.remove();
                    }
                }
            });
        }
    });
})();
