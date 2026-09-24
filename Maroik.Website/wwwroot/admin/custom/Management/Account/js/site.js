"use strict";
(function () {
    const $createAccountTabs = $("#createAccountTabs");
    const $editAccountTabs = $("#editAccountTabs");
    const $gridSearch = $("#gridSearch");
    const $formCreateAccount = $("#formCreateAccount");
    const $createAccountEmail = $("#createAccountEmail");
    const $createAccountPassword = $("#createAccountPassword");
    const $createAccountNickname = $("#createAccountNickname");
    const $createAccountRole = $("#createAccountRole");
    const $createAccountTimeZone = $("#createAccountTimeZone");
    const $__RequestVerificationToken = $("input[name=\"__RequestVerificationToken\"]");
    const $createAccountDialogModal = $("#createAccountDialogModal");
    const $editAccountEmail = $("#editAccountEmail");
    const $editAccountNickname = $("#editAccountNickname");
    const $editAccountRole = $("#editAccountRole");
    const $editAccountTimeZone = $("#editAccountTimeZone");
    const $editAccountLocked = $("#editAccountLocked");
    const $editAccountEmailConfirmed = $("#editAccountEmailConfirmed");
    const $editAccountAgreedServiceTerms = $("#editAccountAgreedServiceTerms");
    const $editMessage = $("#editMessage");
    const $editAccountDeleted = $("#editAccountDeleted");
    const $formEditAccount = $("#formEditAccount");
    const $editAccountPassword = $("#editAccountPassword");
    const $btnEditAccountGridRow = $("#btnEditAccountGridRow");
    const $btnConfirmDeleteAccount = $("#btnConfirmDeleteAccount");
    const $btnExportExcelAccount = $("#btnExportExcelAccount");
    const $btnDeleteAccount = $("#btnDeleteAccount");
    const $editAccountDialogModal = $("#editAccountDialogModal");
    const $confirmDeleteAccountDialogModal = $("#confirmDeleteAccountDialogModal");
    const selectedRowColor = "table-primary";
    $(function () {
        $createAccountTabs.tabs();
        $editAccountTabs.tabs();
    });
    $(document).off("rowclick.Account").on("rowclick.Account", (e) => {
        let selectedRow = e.detail;
        let selectedRowEmail = selectedRow.data.Email;
        let $clsGridRow = $(".clsGridRow");
        $clsGridRow.each(function () {
            $(this).removeClass(selectedRowColor);
        });
        $clsGridRow.each(function () {
            if ((String(selectedRowEmail).valueOf() === String($(this).attr("data-email")).valueOf())) {
                $(this).addClass(selectedRowColor);
            }
        });
    });
    $(document).off("reloadstart.Account").on("reloadstart.Account", _ => {
    });
    $(document).off("reloadend.Account").on("reloadend.Account", _ => {
    });
    $(document).off("reloadfail.Account").on("reloadfail.Account", _ => {
    });
    $(document).off("gridconfigure.Account").on("gridconfigure.Account", _ => {
    });
    $(document).off("dblclick.Account", ".clsGridRow").on("dblclick.Account", ".clsGridRow", function () {
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                EditAccountGridRow();
            }
        });
    });
    $gridSearch.off("input").on("input", function (event) {
        const grid = new MvcGrid(document.querySelector(".mvc-grid"));
        grid.url.searchParams.set("wholeSearch", event.currentTarget.value);
        grid.reload();
    });
    function CreateAccount() {
        if (!$formCreateAccount.valid()) {
            return false;
        }
        let email = $createAccountEmail.val();
        let password = $createAccountPassword.val();
        let nickname = $createAccountNickname.val();
        let role = $createAccountRole.val();
        let timeZoneIanaId = $createAccountTimeZone.val();
        let paramValue = JSON.stringify({
            Email: email,
            Password: password,
            Nickname: nickname,
            Role: role,
            TimeZoneIanaId: timeZoneIanaId
        });
        $.ajax({
            url: "/Management/CreateAccount",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $createAccountDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function EditAccountGridRow(errorMessageSelectGridRow) {
        let selectedRowEmail = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowEmail = $(this).attr("data-email");
            }
        });
        if (selectedRowEmail === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: "/Management/IsAccountExists" + "?email=" + encodeURIComponent(selectedRowEmail),
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editAccountEmail.val(data.account.email);
                    $editAccountNickname.val(data.account.nickname);
                    $editAccountRole.val(data.account.role).trigger("change");
                    $editAccountTimeZone.val(data.account.timeZoneIanaId).trigger("change");
                    $editAccountLocked.prop("checked", data.account.locked);
                    $editAccountEmailConfirmed.prop("checked", data.account.emailConfirmed);
                    $editAccountAgreedServiceTerms.prop("checked", data.account.agreedServiceTerms);
                    $editMessage.val(data.account.message);
                    $editAccountDeleted.prop("checked", data.account.deleted);
                    $editAccountDialogModal.modal({
                        keyboard: false,
                        backdrop: "static"
                    });
                    $editAccountDialogModal.modal("toggle");
                    $editAccountDialogModal.modal("show");
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
    }
    function UpdateAccount() {
        if (!$formEditAccount.valid()) {
            return false;
        }
        let email = $editAccountEmail.val();
        let password = $editAccountPassword.val();
        let role = $editAccountRole.val();
        let timeZoneIanaId = $editAccountTimeZone.val();
        let locked = $editAccountLocked.is(":checked");
        let emailConfirmed = $editAccountEmailConfirmed.is(":checked");
        let agreedServiceTerms = $editAccountAgreedServiceTerms.is(":checked");
        let message = $editMessage.val();
        let deleted = $editAccountDeleted.is(":checked");
        let paramValue = JSON.stringify({
            Email: email,
            Password: password,
            Role: role,
            TimeZoneIanaId: timeZoneIanaId,
            Locked: locked,
            EmailConfirmed: emailConfirmed,
            AgreedServiceTerms: agreedServiceTerms,
            Message: message,
            Deleted: deleted
        });
        $.ajax({
            url: "/Management/UpdateAccount",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editAccountDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function ConfirmDeleteAccount(errorMessageSelectGridRow) {
        let selectedRowEmail = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowEmail = $(this).attr("data-email");
            }
        });
        if (selectedRowEmail === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $confirmDeleteAccountDialogModal.modal({
            keyboard: false,
            backdrop: "static"
        });
        $confirmDeleteAccountDialogModal.modal("toggle");
        $confirmDeleteAccountDialogModal.modal("show");
    }
    function DeleteAccount(errorMessageSelectGridRow) {
        let selectedRowEmail = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowEmail = $(this).attr("data-email");
            }
        });
        if (selectedRowEmail === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: "/Management/IsAccountExists" + "?email=" + encodeURIComponent(selectedRowEmail),
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    let paramValue = JSON.stringify({
                        Email: data.account.email
                    });
                    $.ajax({
                        url: "/Management/DeleteAccount",
                        type: "POST",
                        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                        dataType: "json",
                        data: paramValue,
                        contentType: "application/json; charset=utf-8",
                        success: function (data) {
                            if (data.result) {
                                $confirmDeleteAccountDialogModal.modal("hide");
                                const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                                grid.reload();
                                toastr.success(data.message);
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
    function ExportExcelAccount() {
        let form = document.createElement("form");
        let element1 = document.createElement("input");
        let element2 = document.createElement("input");
        form.method = "POST";
        form.action = "/Management/ExportExcelAccount";
        element1.name = "__RequestVerificationToken";
        element1.value = $__RequestVerificationToken.val();
        form.appendChild(element1);
        element2.name = "fileName";
        element2.value = "Account";
        form.appendChild(element2);
        document.body.appendChild(form);
        form.submit();
    }
    $btnEditAccountGridRow.off("click").on("click", function () {
        EditAccountGridRow($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnConfirmDeleteAccount.off("click").on("click", function () {
        ConfirmDeleteAccount($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnExportExcelAccount.off("click").on("click", function () {
        ExportExcelAccount();
    });
    $btnDeleteAccount.off("click").on("click", function () {
        DeleteAccount($(this).attr("data-errorMessageSelectGridRow"));
    });
    $formCreateAccount.off("submit").on("submit", function () {
        return CreateAccount();
    });
    $formEditAccount.off("submit").on("submit", function () {
        return UpdateAccount();
    });
})();
//# sourceMappingURL=site.js.map