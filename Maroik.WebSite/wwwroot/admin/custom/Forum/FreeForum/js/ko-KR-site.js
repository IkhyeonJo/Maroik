// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$('#writeBoardContent').summernote({
    height: 300,
    lang: 'ko-KR',
    callbacks: {
        onImageUpload: function (files) {
            for (let i = 0; i < files.length; i++) {
                WriteUploadImageFile(files[i]);
            }
        }
    }
});

$('#editBoardContent').summernote({
    height: 300,
    callbacks: {
        onImageUpload: function (files) {
            for (let i = 0; i < files.length; i++) {
                EditUploadImageFile(files[i]);
            }
        }
    }
});

var writeUploadedFile;

var editUploadedFile;

function WriteUploadFile(obj, errorMessage) {
    if (obj.files[0].size > 10485760) {
        alert(errorMessage);
        document.getElementById("writeUploadedFile").value = "";
        return false;
    }
    else {
        writeUploadedFile = obj.files[0];
    }
}

function EditUploadFile(obj, errorMessage) {
    if (obj.files[0].size > 10485760) {
        alert(errorMessage);
        document.getElementById("editUploadedFile").value = "";
        return false;
    }
    else {
        editUploadedFile = obj.files[0];
    }
}

function SearchBoard() {
    window.location.href = "/Forum/FreeForum?searchType=" + $('#searchType').val() + "&searchText=" + $('#searchText').val();
}


function ShowWriteBoardLoading() {
    if (!$('#formWriteBoard').valid()) {
        $('#loading').hide();
        return false;
    }
    else {
        $('#loading').show();
        return true;
    }
}

function ShowEditBoardLoading() {
    if (!$('#formEditBoard').valid()) {
        $('#loading').hide();
        return false;
    }
    else {
        $('#loading').show();
        return true;
    }
}

function WriteUploadImageFile(file) {

    formData = new FormData();
    formData.append("summernoteImageFile", file);

    $.ajax({
        url: "/Forum/UploadImageFile",
        data: formData,
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        type: 'POST',
        enctype: 'multipart/form-data',
        processData: false,
        contentType: false,
        dataType: 'json',
        cache: false,
        success: function (data) {
            if (data.result) {
                var imgNode = document.createElement('img');
                imgNode.src = data.filePath;
                imgNode.style.maxWidth = "170px";
                imgNode.style.maxHeight = "209px";
                $('#writeBoardContent').summernote('insertNode', imgNode);
            }
            else {
                alert(data.errorMessage);
            }
        }
    });
}

function EditUploadImageFile(file) {

    formData = new FormData();
    formData.append("summernoteImageFile", file);

    $.ajax({
        url: "/Forum/UploadImageFile",
        data: formData,
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        type: 'POST',
        enctype: 'multipart/form-data',
        processData: false,
        contentType: false,
        dataType: 'json',
        cache: false,
        success: function (data) {
            if (data.result) {
                var imgNode = document.createElement('img');
                imgNode.src = data.filePath;
                imgNode.style.maxWidth = "170px";
                imgNode.style.maxHeight = "209px";
                $('#editBoardContent').summernote('insertNode', imgNode);
            }
            else {
                alert(data.errorMessage);
            }
        }
    });
}

function WriteBoard() {

    if (!$('#formWriteBoard').valid()) {
        return false;
    }

    var createForm = $('#formWriteBoard');

    var title = createForm.find('input[id="writeBoardTitle"]').val();
    var content = createForm.find('textarea[id="writeBoardContent"]').val();
    var noticed = createForm.find('input[id="writeBoardNoticed"]').is(":checked");

    var formData = new FormData();
    formData.append('Title', title);
    formData.append('Content', content);
    formData.append('Noticed', noticed);
    formData.append('UploadedFile', writeUploadedFile);

    $.ajax({
        url: '/Forum/WriteFreeBoard',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: formData,
        contentType: false,
        processData: false,
        success: function (data) {
            if (data.result) {
                alert(data.message);
                window.location.href = "/Forum/FreeForum";
            }
            else {
                toastr.error(data.error);
                $('#loading').hide();
            }
        }
    });

    return false;
}

function EditBoard(editBoardId) {

    if (!$('#formEditBoard').valid()) {
        return false;
    }

    var createForm = $('#formEditBoard');

    var title = createForm.find('input[id="editBoardTitle"]').val();
    var content = createForm.find('textarea[id="editBoardContent"]').val();
    var locked = createForm.find('input[id="editBoardLocked"]').is(":checked");

    var formData = new FormData();
    formData.append('Id', editBoardId);
    formData.append('Title', title);
    formData.append('Locked', locked);
    formData.append('Content', content);
    formData.append('UploadedFile', editUploadedFile);


    $.ajax({
        url: '/Forum/EditFreeBoard',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: formData,
        contentType: false,
        processData: false,
        success: function (data) {
            if (data.result) {
                alert(data.message);
                window.location.href = "/Forum/FreeForum";
            }
            else {
                toastr.error(data.error);
                $('#loading').hide();
            }
        }
    });

    return false;
}

function ConfirmDeleteBoard() {
    $('#confirmDeleteBoardDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteBoardDialogModal').modal('toggle');
    $('#confirmDeleteBoardDialogModal').modal('show');
}

function DeleteBoard(detailBoardId) {

    $.ajax({
        url: '/Forum/IsBoardExists' + '?id=' + detailBoardId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                var paramValue = JSON.stringify({
                    Id: data.freeBoard.id
                });

                $.ajax({
                    url: '/Forum/DeleteBoard',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    //async: false,
                    //cache: false,
                    success: function (data) {
                        if (data.result) {
                            $('#confirmDeleteBoardDialogModal').modal('hide');
                            alert(data.message);
                            window.location.href = "/Forum/FreeForum";
                        }
                        else {
                            toastr.error(data.error);
                        }
                    }
                });
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function WriteFreeComment(detailBoardId, detailCurrentPage, errorMessage) {

    var createForm = $('#formWriteFreeComment');
    var content = createForm.find('input[id="writeFreeCommentContent"]').val();

    if (!content) {
        alert(errorMessage);
        return false;
    }

    var paramValue = JSON.stringify({
        BoardId: detailBoardId,
        Content: content,
        DetailCurrentPage: detailCurrentPage
    });

    $.ajax({
        url: '/Forum/WriteFreeComment',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                window.location.href = "/Forum/FreeForum?method=detail" + "&boardId=" + data.boardId + "&page=" + data.page;
            }
            else {
                toastr.error(data.error);
            }
        }
    });

    return false;
}
function DeleteComment(commentId, boardId, page) {
    $.ajax({
        url: '/Forum/DeleteComment' + '?id=' + commentId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                window.location.href = "/Forum/FreeForum?method=detail" + "&boardId=" + boardId + "&page=" + page;
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}