// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let selectedRowColor = "table-primary";

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.

    let selectedRow = e.detail;
    let selectedRowId = selectedRow.data.Id;

    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {
        if ((new String(selectedRowId).valueOf() === new String($(this).attr('data-id')).valueOf())) {
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


$(document).on("dblclick", "#incomeGrid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            EditIncomeGridRow();
        }
    });
    //console.log('dblclick-end');
});
$('#gridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("wholeSearch", this.value);

    grid.reload();
});

function CreateFormShowIncomeSubClassBySelectedIncomeMainClass(createIncomeMainClass) {

    if (createIncomeMainClass.value === "RegularIncome") {
        $('#createIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $("#createIncomeSubClass option:selected").removeAttr("selected");

        $('#createIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#createIncomeSubClass option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
        $('#createIncomeSubClass option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
        $('#createIncomeSubClass option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
        $('#createIncomeSubClass option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
        $('#createIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $("#createIncomeSubClass").val("LaborIncome").change();
    }
    else if (createIncomeMainClass.value === "IrregularIncome") {
        $('#createIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $('#createIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $("#createIncomeSubClass option:selected").removeAttr("selected");

        $('#createIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#createIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $("#createIncomeSubClass").val("LaborIncome").change();
    }
}

function EditFormShowIncomeSubClassBySelectedIncomeMainClass(createIncomeMainClass) {

    if (createIncomeMainClass.value === "RegularIncome") {
        $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $("#editIncomeSubClass option:selected").removeAttr("selected");

        $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
        $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
        $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
        $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
        $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $("#editIncomeSubClass").val("LaborIncome").change();
    }
    else if (createIncomeMainClass.value === "IrregularIncome") {
        $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $("#editIncomeSubClass option:selected").removeAttr("selected");

        $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $("#editIncomeSubClass").val("LaborIncome").change();
    }
}

function CreateIncome() {

    if (!$('#formCreateIncome').valid()) {
        return false;
    }

    let createForm = $('#formCreateIncome');

    let mainClass = createForm.find('select[id="createIncomeMainClass"]').val();
    let subClass = createForm.find('select[id="createIncomeSubClass"]').val();
    let content = createForm.find('input[id="createIncomeContent"]').val();
    let amount = createForm.find('input[id="createIncomeAmount"]').val();

    let year = parseInt(createForm.find('input[id="createIncomeDate"]').val().substring(0, 4));
    let month = parseInt(createForm.find('input[id="createIncomeDate"]').val().substring(5, 7));
    let day = parseInt(createForm.find('input[id="createIncomeDate"]').val().substring(8, 10));
    let hour = parseInt(createForm.find('select[id="createIncomeHour"]').val());
    let minute = parseInt(createForm.find('select[id="createIncomeMinute"]').val());
    let second = parseInt(createForm.find('select[id="createIncomeSecond"]').val());

    let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
    let depositMyAssetProductName = createForm.find('select[id="createIncomeDepositMyAssetProductName"]').val();
    let note = createForm.find('input[id="createIncomeNote"]').val();

    let paramValue = JSON.stringify({
        MainClass: mainClass,
        SubClass: subClass,
        Content: content,
        Amount: amount,
        Created: created,
        DepositMyAssetProductName: depositMyAssetProductName,
        Note: note
    });

    $.ajax({
        url: '/AccountBook/CreateIncome',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createIncomeDialogModal').modal('hide'); // hide Modal

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

function EditIncomeGridRow(errorMessageSelectGridRow) {

    let selectedRowId = "";

    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsIncomeExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                let formEditIncome = $('#formEditIncome');

                formEditIncome.find('input[id="editIncomeId"]').val(data.income.id);
                formEditIncome.find('select[id="editIncomeMainClass"]').val(data.income.mainClass).change();

                if (data.income.mainClass === "RegularIncome") {
                    $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

                    $("#editIncomeSubClass option:selected").removeAttr("selected");

                    $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

                    formEditIncome.find('select[id="editIncomeSubClass"]').val(data.income.subClass).change();
                }
                else if (data.income.mainClass === "IrregularIncome") {
                    $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
                    $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

                    $("#editIncomeSubClass option:selected").removeAttr("selected");

                    $('#editIncomeSubClass option[value=LaborIncome]').prop('disabled', false).prop('hidden', false);
                    $('#editIncomeSubClass option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

                    formEditIncome.find('select[id="editIncomeSubClass"]').val(data.income.subClass).change();
                }

                formEditIncome.find('input[id="editIncomeContent"]').val(data.income.content);
                formEditIncome.find('input[id="editIncomeAmount"]').val(data.income.amount);
                formEditIncome.find('input[id="editIncomeDate"]').val(data.income.created.split('T')[0]);
                formEditIncome.find('select[id="editIncomeHour"]').val(parseInt(data.income.created.split('T')[1].substring(0, 2))).change();
                formEditIncome.find('select[id="editIncomeMinute"]').val(parseInt(data.income.created.split('T')[1].substring(3, 5))).change();
                formEditIncome.find('select[id="editIncomeSecond"]').val(parseInt(data.income.created.split('T')[1].substring(6, 8))).change();
                formEditIncome.find('select[id="editIncomeDepositMyAssetProductName"]').val(data.income.depositMyAssetProductName).change();
                formEditIncome.find('input[id="editIncomeNote"]').val(data.income.note);

                $('#editIncomeDialogModal').modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $('#editIncomeDialogModal').modal('toggle');
                $('#editIncomeDialogModal').modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function UpdateIncome() {

    if (!$('#formEditIncome').valid()) {
        return false;
    }

    let createForm = $('#formEditIncome');

    let id = createForm.find('input[id="editIncomeId"]').val();
    let mainClass = createForm.find('select[id="editIncomeMainClass"]').val();
    let subClass = createForm.find('select[id="editIncomeSubClass"]').val();
    let content = createForm.find('input[id="editIncomeContent"]').val();
    let amount = createForm.find('input[id="editIncomeAmount"]').val();

    let year = parseInt(createForm.find('input[id="editIncomeDate"]').val().substring(0, 4));
    let month = parseInt(createForm.find('input[id="editIncomeDate"]').val().substring(5, 7));
    let day = parseInt(createForm.find('input[id="editIncomeDate"]').val().substring(8, 10));
    let hour = parseInt(createForm.find('select[id="editIncomeHour"]').val());
    let minute = parseInt(createForm.find('select[id="editIncomeMinute"]').val());
    let second = parseInt(createForm.find('select[id="editIncomeSecond"]').val());

    let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
    let depositMyAssetProductName = createForm.find('select[id="editIncomeDepositMyAssetProductName"]').val();
    let note = createForm.find('input[id="editIncomeNote"]').val();

    let paramValue = JSON.stringify({
        Id: id,
        MainClass: mainClass,
        SubClass: subClass,
        Content: content,
        Amount: amount,
        Created: created,
        DepositMyAssetProductName: depositMyAssetProductName,
        Note: note
    });

    $.ajax({
        url: '/AccountBook/UpdateIncome',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#editIncomeDialogModal').modal('hide'); // hide Modal

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

function ConfirmDeleteIncome(errorMessageSelectGridRow) {

    let selectedRowId = "";

    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $('#confirmDeleteIncomeDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteIncomeDialogModal').modal('toggle');
    $('#confirmDeleteIncomeDialogModal').modal('show');
}

function DeleteIncome(errorMessageSelectGridRow) {

    let selectedRowId = "";

    $('#incomeGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsIncomeExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                let paramValue = JSON.stringify({
                    Id: data.income.id
                });

                $.ajax({
                    url: '/AccountBook/DeleteIncome',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    //async: false,
                    //cache: false,
                    success: function (data) {
                        if (data.result) {
                            $('#confirmDeleteIncomeDialogModal').modal('hide'); // hide Modal

                            const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                            grid.reload(); //reload grid

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

function ExportExcelIncome() {
    let form = document.createElement("form");
    let element1 = document.createElement("input");
    let element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/AccountBook/ExportExcelIncome";

    element1.name = "__RequestVerificationToken";
    element1.value = $('input[name="__RequestVerificationToken"]').val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "Income";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}

function ChangeCreateIncomeAmountLabel(productName) {
    $.ajax({
        url: '/AccountBook/GetIncomeAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#labelCreateIncomeAmount').text(data.label);
            }
            else {
                $('#labelCreateIncomeAmount').text(data.label);
            }
        }
    });
}

function ChangeEditIncomeAmountLabel(productName) {
    $.ajax({
        url: '/AccountBook/GetIncomeAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#labelEditIncomeAmount').text(data.label);
            }
            else {
                $('#labelEditIncomeAmount').text(data.label);
            }
        }
    });
}

$('#btnEditIncomeGridRow').off('click').on('click', function () {
    EditIncomeGridRow($(this).attr("data-errorMessageSelectGridRow"));
});

$('#btnConfirmDeleteIncome').off('click').on('click', function () {
    ConfirmDeleteIncome($(this).attr("data-errorMessageSelectGridRow"));
});

$('#btnExportExcelIncome').off('click').on('click', function () {
    ExportExcelIncome();
});

$('#btnDeleteIncome').off('click').on('click', function () {
    DeleteIncome($(this).attr("data-errorMessageSelectGridRow"));
});

$('#formCreateIncome').off('submit').on('submit', function () {
    return CreateIncome();
});

$('#formEditIncome').off('submit').on('submit', function () {
    return UpdateIncome();
});

$('#createIncomeMainClass').off('change').on('change', function () {
    CreateFormShowIncomeSubClassBySelectedIncomeMainClass(this);
});

$('#editIncomeMainClass').off('change').on('change', function () {
    EditFormShowIncomeSubClassBySelectedIncomeMainClass(this);
});

$('#createIncomeDepositMyAssetProductName').off('change').on('change', function () {
    ChangeCreateIncomeAmountLabel(this.value);
});

$('#editIncomeDepositMyAssetProductName').off('change').on('change', function () {
    ChangeEditIncomeAmountLabel(this.value);
});

$(function () {
    ChangeCreateIncomeAmountLabel($('#createIncomeDepositMyAssetProductName option:selected').val());
})