// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $("#createIncomeTabs").tabs();
    $("#editAssetTabs").tabs();
});

var selectedRowColor = "table-primary";

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.

    var selectedRow = e.detail;
    var selectedRowProductName = selectedRow.data.ProductName;

    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {
        if ((new String(selectedRowProductName).valueOf() === new String($(this).attr('data-productName')).valueOf())) {
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


$(document).on("dblclick", "#assetGrid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            EditAssetGridRow();
        }
    });
    //console.log('dblclick-end');
});
$('#gridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("wholeSearch", this.value);

    grid.reload();
});

function ShowIncomeSubClassBySelectedIncomeMainClass(createIncomeMainClass) {
    
    if (createIncomeMainClass.value == "RegularIncome") {
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
    else if (createIncomeMainClass.value == "IrregularIncome") {
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

function CreateIncome() {

    if (!$('#formCreateIncome').valid()) {
        return false;
    }

    var createForm = $('#formCreateIncome');

    var mainClass = createForm.find('select[id="createIncomeMainClass"]').val();
    var subClass = createForm.find('select[id="createIncomeSubClass"]').val();
    var contents = createForm.find('input[id="createIncomeContents"]').val();
    var amount = createForm.find('input[id="createIncomeAmount"]').val();
    var depositMyAssetProductName = createForm.find('select[id="createIncomeDepositMyAssetProductName"]').val();
    var note = createForm.find('input[id="createIncomeNote"]').val();

    var paramValue = JSON.stringify({
        MainClass: mainClass,
        SubClass: subClass,
        Contents: contents,
        Amount: amount,
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

function EditAssetGridRow(errorMessageSelectGridRow) {

    var selectedRowProductName = "";

    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowProductName = $(this).attr("data-productName");
        }
    }); // Search Selected Row

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsAssetExists' + '?productName=' + selectedRowProductName,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                var formEditAsset = $('#formEditAsset');

                formEditAsset.find('input[id="editAssetProductName"]').val(data.asset.productName);
                formEditAsset.find('input[id="editAssetAmount"]').val(data.asset.amount);
                formEditAsset.find('input[id="editAssetNote"]').val(data.asset.note);
                formEditAsset.find('input[id="editAssetDeleted"]').prop("checked", data.asset.deleted);

                $('#editAssetDialogModal').modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $('#editAssetDialogModal').modal('toggle');
                $('#editAssetDialogModal').modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function UpdateAsset() {

    if (!$('#formEditAsset').valid()) {
        return false;
    }

    var createForm = $('#formEditAsset');

    var productName = createForm.find('input[id="editAssetProductName"]').val();
    var amount = createForm.find('input[id="editAssetAmount"]').val();
    var note = createForm.find('input[id="editAssetNote"]').val();
    var deleted = createForm.find('input[id="editAssetDeleted"]').is(":checked");

    var paramValue = JSON.stringify({
        ProductName: productName,
        Amount: amount,
        Note: note,
        Deleted: deleted
    });

    $.ajax({
        url: '/AccountBook/UpdateAsset',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#editAssetDialogModal').modal('hide'); // hide Modal

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

function ConfirmDeleteAsset(errorMessageSelectGridRow) {

    var selectedRowProductName = "";

    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowProductName = $(this).attr("data-productName");
        }
    }); // Search Selected Row

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $('#confirmDeleteAssetDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteAssetDialogModal').modal('toggle');
    $('#confirmDeleteAssetDialogModal').modal('show');
}

function DeleteAsset(errorMessageSelectGridRow) {

    var selectedRowProductName = "";

    $('#assetGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowProductName = $(this).attr("data-productName");
        }
    }); // Search Selected Row

    if (selectedRowProductName === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsAssetExists' + '?productName=' + selectedRowProductName,
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
                    ProductName: data.asset.productName
                });

                $.ajax({
                    url: '/AccountBook/DeleteAsset',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    //async: false,
                    //cache: false,
                    success: function (data) {
                        if (data.result) {
                            $('#confirmDeleteAssetDialogModal').modal('hide'); // hide Modal

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

function ExportExcelAsset() {
    var form = document.createElement("form");
    var element1 = document.createElement("input");
    var element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/AccountBook/ExportExcelAsset";

    element1.name = "__RequestVerificationToken";
    element1.value = $('input[name="__RequestVerificationToken"]').val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "Asset";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}