let $createAssetTabs = $("#createAssetTabs");
let $editAssetTabs = $("#editAssetTabs");
let $gridSearch = $('#gridSearch');
let $formCreateAsset = $('#formCreateAsset');
let $createAssetProductName = $('#createAssetProductName');
let $createAssetItem = $('#createAssetItem');
let $createAssetAmount = $('#createAssetAmount');
let $createAssetMonetaryUnit = $('#createAssetMonetaryUnit');
let $createAssetNote = $('#createAssetNote');
let $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
let $createAssetDialogModal = $('#createAssetDialogModal');
let $editAssetOriginalProductName = $('#editAssetOriginalProductName');
let $editAssetProductName = $('#editAssetProductName');
let $editAssetItem = $('#editAssetItem');
let $editAssetAmount = $('#editAssetAmount');
let $editAssetMonetaryUnit = $('#editAssetMonetaryUnit');
let $editAssetNote = $('#editAssetNote');
let $editAssetDeleted = $('#editAssetDeleted');
let $editAssetDialogModal = $('#editAssetDialogModal');
let $formEditAsset = $('#formEditAsset');
let $confirmDeleteAssetDialogModal = $('#confirmDeleteAssetDialogModal');
let $btnEditAssetGridRow = $('#btnEditAssetGridRow');
let $btnConfirmDeleteAsset = $('#btnConfirmDeleteAsset');
let $btnExportExcelAsset = $('#btnExportExcelAsset');
let $btnDeleteAsset = $('#btnDeleteAsset');

$(function () {
    $createAssetTabs.tabs();
    $editAssetTabs.tabs();
});

let selectedRowColor = "table-primary";

$(document).off('rowclick.Asset').on('rowclick.Asset', e => {
    let selectedRow = e.detail;
    let selectedRowProductName = selectedRow.data.ProductName;
    let $clsGridRow = $('.clsGridRow');

    $clsGridRow.each(function () {
        $(this).removeClass(selectedRowColor);
    });

    $clsGridRow.each(function () {
        if ((String(selectedRowProductName).valueOf() === String($(this).attr('data-productName')).valueOf())) {
            $(this).addClass(selectedRowColor);
        }
    });
});

$(document).off('reloadstart.Asset').on('reloadstart.Asset', _ => {
});

$(document).off('reloadend.Asset').on('reloadend.Asset', _ => {
});

$(document).off('reloadfail.Asset').on('reloadfail.Asset', _ => {
});

$(document).off('gridconfigure.Asset').on('gridconfigure.Asset', _ => {
});

$(document).off('dblclick.Asset', '.clsGridRow').on('dblclick.Asset', '.clsGridRow', function () {
    $('.clsGridRow').each(function () {
        if ($(this).attr("class").includes(selectedRowColor)) {
            EditAssetGridRow();
        }
    });
});

$gridSearch.off('input').on('input', function () {
    const grid = new MvcGrid(document.querySelector('.mvc-grid'));
    grid.url.searchParams.set("wholeSearch", this.value);
    grid.reload();
});

function CreateAsset() {

    if (!$formCreateAsset.valid()) {
        return false;
    }

    let productName = $createAssetProductName.val();
    let item = $createAssetItem.val();
    let amount = $createAssetAmount.val();
    let monetaryUnit = $createAssetMonetaryUnit.val();
    let note = $createAssetNote.val();

    let paramValue = JSON.stringify({
        ProductName: productName,
        Item: item,
        Amount: amount,
        MonetaryUnit: monetaryUnit,
        Note: note
    });

    $.ajax({
        url: '/AccountBook/CreateAsset',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $createAssetDialogModal.modal('hide');

                const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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

function EditAssetGridRow(errorMessageSelectGridRow) {

    let selectedRowProductName = "";

    $('.clsGridRow').each(function () {
        if ($(this).attr("class").includes(selectedRowColor)) {
            selectedRowProductName = $(this).attr("data-productName");
        }
    });

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    }

    $.ajax({
        url: '/AccountBook/IsAssetExists' + '?productName=' + selectedRowProductName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                $editAssetOriginalProductName.val(data.asset.productName);
                $editAssetProductName.val(data.asset.productName);
                $editAssetItem.val(data.asset.item).change();
                $editAssetAmount.val(data.asset.amount);
                $editAssetMonetaryUnit.val(data.asset.monetaryUnit);
                $editAssetNote.val(data.asset.note);
                $editAssetDeleted.prop("checked", data.asset.deleted);

                $editAssetDialogModal.modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $editAssetDialogModal.modal('toggle');
                $editAssetDialogModal.modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function UpdateAsset() {

    if (!$formEditAsset.valid()) {
        return false;
    }

    let productName = $editAssetProductName.val();
    let item = $editAssetItem.val();
    let amount = $editAssetAmount.val();
    let monetaryUnit = $editAssetMonetaryUnit.val();
    let note = $editAssetNote.val();
    let deleted = $editAssetDeleted.is(":checked");
    let originalProductName = $editAssetOriginalProductName.val();

    let paramValue = JSON.stringify({
        ProductName: productName,
        Item: item,
        Amount: amount,
        MonetaryUnit: monetaryUnit,
        Note: note,
        Deleted: deleted,
        OriginalProductName: originalProductName
    });

    $.ajax({
        url: '/AccountBook/UpdateAsset',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $editAssetDialogModal.modal('hide');

                const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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

function ConfirmDeleteAsset(errorMessageSelectGridRow) {

    let selectedRowProductName = "";

    $('.clsGridRow').each(function () {
        if ($(this).attr("class").includes(selectedRowColor)) {
            selectedRowProductName = $(this).attr("data-productName");
        }
    });

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    }

    $confirmDeleteAssetDialogModal.modal({
        keyboard: false,
        backdrop: "static"
    });

    $confirmDeleteAssetDialogModal.modal('toggle');
    $confirmDeleteAssetDialogModal.modal('show');
}

function DeleteAsset(errorMessageSelectGridRow) {

    let selectedRowProductName = "";

    $('.clsGridRow').each(function () {
        if ($(this).attr("class").includes(selectedRowColor)) {
            selectedRowProductName = $(this).attr("data-productName");
        }
    });

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    }

    $.ajax({
        url: '/AccountBook/IsAssetExists' + '?productName=' + selectedRowProductName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                let paramValue = JSON.stringify({
                    ProductName: data.asset.productName
                });

                $.ajax({
                    url: '/AccountBook/DeleteAsset',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    success: function (data) {
                        if (data.result) {
                            $confirmDeleteAssetDialogModal.modal('hide');

                            const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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

function ExportExcelAsset() {
    let form = document.createElement("form");
    let element1 = document.createElement("input");
    let element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/AccountBook/ExportExcelAsset";

    element1.name = "__RequestVerificationToken";
    element1.value = $__RequestVerificationToken.val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "Asset";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}

$btnEditAssetGridRow.off('click').on('click', function () {
    EditAssetGridRow($(this).attr("data-errorMessageSelectGridRow"));
});

$btnConfirmDeleteAsset.off('click').on('click', function () {
    ConfirmDeleteAsset($(this).attr("data-errorMessageSelectGridRow"));
});

$btnExportExcelAsset.off('click').on('click', function () {
    ExportExcelAsset();
});

$btnDeleteAsset.off('click').on('click', function () {
    DeleteAsset($(this).attr("data-errorMessageSelectGridRow"));
});

$formCreateAsset.off('submit').on('submit', function () {
    return CreateAsset();
});

$formEditAsset.off('submit').on('submit', function () {
    return UpdateAsset();
});