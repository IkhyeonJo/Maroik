"use strict";
(function () {
    const $formUpdateProfilePassword = $("#formUpdateProfilePassword");
    const $formUpdateProfileAvatar = $("#formUpdateProfileAvatar");
    const $profileAvatarFiles = $("#ProfileAvatarFiles");
    const $newPassword = $("#NewPassword");
    const $password = $("#Password");
    const $__RequestVerificationToken = $("input[name=\"__RequestVerificationToken\"]");
    const maxFileSize = parseInt($("#maxAttachedFileSizeBytes").val()) || window.MaroikDefaultMaxAttachedFileSizeBytes;
    const allowedImageContentTypes = (JSON.parse($("#allowedImageContentTypes").val() || "[]")).map(function (t) {
        return t.toLowerCase();
    });
    function UpdateProfileAvatar(noFileAttachedErrorMessage, fileSizeLimitationErrorMessage, fileTypeErrorMessage) {
        let files = document.getElementById("ProfileAvatarFiles").files[0];
        if (files === undefined || files === null) {
            alert(noFileAttachedErrorMessage);
            window.location.href = "/Management/Profile";
        }
        else {
            if (files.size > maxFileSize) {
                alert(fileSizeLimitationErrorMessage);
                window.location.href = "/Management/Profile";
            }
            else if (files.size > 0 && files.size <= maxFileSize) {
                if (allowedImageContentTypes.indexOf((files.type || "").toLowerCase()) !== -1) {
                    let form = $formUpdateProfileAvatar[0];
                    let formData = new FormData(form);
                    $.ajax({
                        url: "/Management/UpdateProfileAvatar",
                        data: formData,
                        type: "POST",
                        enctype: "multipart/form-data",
                        processData: false,
                        contentType: false,
                        dataType: "json",
                        cache: false,
                        success: function (data) {
                            if (data.result) {
                                window.location.href = "/Management/Profile";
                            }
                            else {
                                alert(data.errorMessage);
                                window.location.href = "/Management/Profile";
                            }
                        }
                    });
                }
                else {
                    alert(fileTypeErrorMessage);
                    window.location.href = "/Management/Profile";
                }
            }
            else {
                alert(fileSizeLimitationErrorMessage);
                window.location.href = "/Management/Profile";
            }
        }
    }
    function UpdateProfilePassword() {
        if (!$formUpdateProfilePassword.valid()) {
            return false;
        }
        let password = $password.val();
        let newPassword = $newPassword.val();
        let paramValue = JSON.stringify({
            Password: password,
            NewPassword: newPassword
        });
        $.ajax({
            url: "/Management/UpdateProfilePassword",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    alert(data.message);
                    window.location.href = "/Account/Login";
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    $formUpdateProfilePassword.off("submit").on("submit", function () {
        return UpdateProfilePassword();
    });
    $profileAvatarFiles.off("change").on("change", function () {
        return UpdateProfileAvatar($(this).attr("data-noFileAttachedErrorMessage"), $(this).attr("data-fileSizeLimitationErrorMessage"), $(this).attr("data-fileTypeErrorMessage"));
    });
})();
