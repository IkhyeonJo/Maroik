// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $("#createAccountTabs").tabs();
    $("#editAccountTabs").tabs();
});

var selectedRowColor = "table-primary";

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.

    var selectedRow = e.detail;
    var selectedRowEmail = selectedRow.data.Email;

    $('#accountGrid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#accountGrid > table > tbody  > tr').each(function (index, tr) {
        if ((new String(selectedRowEmail).valueOf() === new String($(this).attr('data-email')).valueOf())) {
            $(this).addClass(selectedRowColor);
        }
    }); // set selected row color class
});

// Triggered before grid starts loading. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadstart", e => {
    //e.detail.grid - grid's instance.
});

// Triggered after grid stop loading. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadend", e => {
    //e.detail.grid - grid's instance.
});

// Triggered after grid reload fails. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadfail", e => {
    //e.detail.reason - failed response promise reason.
    //e.detail.grid - grid's instance.

    // Preventing default stops failed promise from bubbling out.
});

// Triggered after grid configuration changes. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("gridconfigure", e => {
    //e.detail.grid - grid's instance.
});


$(document).on("dblclick", "#accountGrid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#accountGrid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            EditAccountGridRow();
        }
    });
    //console.log('dblclick-end');
});
$('#gridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("wholeSearch", this.value);

    grid.reload();
});

function CreateAccount() {

    if (!$('#formCreateAccount').valid()) {
        return false;
    }

    var createForm = $('#formCreateAccount');

    var email = createForm.find('input[id="createAccountEmail"]').val();
    var password = createForm.find('input[id="createAccountPassword"]').val();
    var fullName = createForm.find('input[id="createAccountFullName"]').val();
    var role = createForm.find('select[id="createAccountRole"]').val();

    var paramValue = JSON.stringify({
        Email: email,
        Password: password,
        FullName: fullName,
        Role: role
    });

    $.ajax({
        url: '/Management/CreateAccount',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createAccountDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

                toastr.success(data.message);
            }
            else {
                toastr.error(data.error);
            }
        }
    });

    return false;
}

function EditAccountGridRow() {

    var selectedRowEmail = "";

    $('#accountGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowEmail = $(this).attr("data-email");
        }
    }); // Search Selected Row

    if (selectedRowEmail === "") {
        toastr.error("Please select grid row");
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/Management/IsAccountExists' + '?email=' + selectedRowEmail,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                var formEditAccount = $('#formEditAccount');

                formEditAccount.find('input[id="editAccountEmail"]').val(data.account.email);
                formEditAccount.find('input[id="editAccountFullName"]').val(data.account.fullName);
                formEditAccount.find('select[id="editAccountRole"]').val(data.account.role).change();
                formEditAccount.find('input[id="editAccountLocked"]').prop("checked", data.account.locked);
                formEditAccount.find('input[id="editAccountEmailConfirmed"]').prop("checked", data.account.emailConfirmed);
                formEditAccount.find('input[id="editAccountAgreedServiceTerms"]').prop("checked", data.account.agreedServiceTerms);
                formEditAccount.find('input[id="editAccountMessage"]').val(data.account.message);
                formEditAccount.find('input[id="editAccountDeleted"]').prop("checked", data.account.deleted);

                $('#editAccountDialogModal').modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $('#editAccountDialogModal').modal('toggle');
                $('#editAccountDialogModal').modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function UpdateAccount() {
    if (!$('#formEditAccount').valid()) {
        return false;
    }

    var createForm = $('#formEditAccount');

    var email = createForm.find('input[id="editAccountEmail"]').val();
    var password = createForm.find('input[id="editAccountPassword"]').val();
    var fullName = createForm.find('input[id="editAccountFullName"]').val();
    var role = createForm.find('select[id="editAccountRole"]').val();
    var locked = createForm.find('input[id="editAccountLocked"]').is(":checked");
    var emailConfirmed = createForm.find('input[id="editAccountEmailConfirmed"]').is(":checked");
    var agreedServiceTerms = createForm.find('input[id="editAccountAgreedServiceTerms"]').is(":checked");
    var accountMessage = createForm.find('input[id="editAccountMessage"]').val();
    var deleted = createForm.find('input[id="editAccountDeleted"]').is(":checked");

    var paramValue = JSON.stringify({
        Email: email,
        Password: password,
        FullName: fullName,
        Role: role,
        Locked: locked,
        EmailConfirmed: emailConfirmed,
        AgreedServiceTerms: agreedServiceTerms,
        AccountMessage: accountMessage,
        Deleted: deleted
    });

    return false;

    $.ajax({
        url: '/Management/UpdateCategory',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#editAccountDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

                toastr.success('Successfully updated the category');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
    return false;
}

function ConfirmDeleteMenu() {

    var selectedRowId = "-1";

    $('#menuGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "-1") {
        toastr.error("Please select grid row");
        return false;
    } // check whether selected row exist or not

    $('#confirmDeleteMenuDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteMenuDialogModal').modal('toggle');
    $('#confirmDeleteMenuDialogModal').modal('show');
}

function DeleteMenu() {
    var selectedRowId = "-1";
    var selectedRowCategoryId = "-1";

    $('#menuGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
            selectedRowCategoryId = $(this).attr("data-categoryid");
        }
    }); // Search Selected Row

    if (selectedRowId === "-1") {
        toastr.error("Please select grid row");
        return false;
    } // check whether selected row exist or not

    if (selectedRowCategoryId === "-1") {

        $.ajax({
            url: '/Management/IsCategoryExists' + '?id=' + selectedRowId,
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
                        Id: data.category.id,
                        Name: data.category.name,
                        DisplayName: data.category.displayName,
                        IconPath: data.category.iconPath,
                        Controller: data.category.controller,
                        Action: data.category.action,
                        Role: data.category.role,
                        Order: data.category.order
                    });

                    $.ajax({
                        url: '/Management/DeleteCategory',
                        type: 'POST',
                        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                        dataType: 'json',
                        data: paramValue,
                        contentType: 'application/json; charset=utf-8',
                        //async: false,
                        //cache: false,
                        success: function (data) {
                            if (data.result) {
                                $('#confirmDeleteMenuDialogModal').modal('hide'); // hide Modal

                                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                                grid.reload(); //reload grid

                                toastr.success('Successfully deleted the category');
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
    } // Category
    else {
        $.ajax({
            url: '/Management/IsSubCategoryExists' + '?id=' + selectedRowId,
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
                        Id: data.subCategory.id,
                        CategoryId: data.subCategory.categoryId,
                        Name: data.subCategory.name,
                        DisplayName: data.subCategory.displayName,
                        IconPath: data.subCategory.iconPath,
                        Action: data.subCategory.action,
                        Role: data.subCategory.role,
                        Order: data.subCategory.order
                    });

                    $.ajax({
                        url: '/Management/DeleteSubCategory',
                        type: 'POST',
                        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                        dataType: 'json',
                        data: paramValue,
                        contentType: 'application/json; charset=utf-8',
                        //async: false,
                        //cache: false,
                        success: function (data) {
                            if (data.result) {
                                $('#confirmDeleteMenuDialogModal').modal('hide'); // hide Modal

                                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                                grid.reload(); //reload grid

                                toastr.success('Successfully deleted the subCategory');
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
    } // SubCategory
}

function ExportExcelMenu() {
    var form = document.createElement("form");
    var element1 = document.createElement("input");
    var element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/Management/ExportExcelMenu";

    element1.name = "__RequestVerificationToken";
    element1.value = $('input[name="__RequestVerificationToken"]').val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "Menu";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}