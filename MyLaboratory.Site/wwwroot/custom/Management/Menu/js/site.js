// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $("#tabs").tabs();
});

var selectedRowColor = "table-primary";

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.

    var selectedRow = e.detail;
    var selectedRowId = selectedRow.data.Id;
    var selectedRowCategoryId = selectedRow.data.CategoryId;

    if (selectedRowCategoryId === "") {
        selectedRowCategoryId = "-1";
    }

    $('#menuGrid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#menuGrid > table > tbody  > tr').each(function (index, tr) {
        if ((new String(selectedRowId).valueOf() === new String($(this).attr('data-id')).valueOf()) && (new String(selectedRowCategoryId).valueOf() === new String($(this).attr('data-categoryid')).valueOf())) {
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


$(document).on("dblclick", "#menuGrid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#menuGrid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            console.log("SelectedRow Id:" + $(this).attr("data-id"));
            //pop Modal or something... 
        }
    });
    //console.log('dblclick-end');
});
$('#gridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("wholeSearch", this.value);

    grid.reload();
});

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

function CreateCategory() {

    if (!$('#formCreateCategory').valid()) {
        return false;
    }

    var createForm = $('#formCreateCategory');
    var name = createForm.find('input[id="categoryName"]').val();
    var displayName = createForm.find('input[id="categoryDisplayName"]').val();
    var iconPath = createForm.find('input[id="categoryIconPath"]').val();
    var controller = createForm.find('input[id="categoryController"]').val();
    var action = createForm.find('input[id="categoryAction"]').val();
    var role = createForm.find('select[id="categoryRole"]').val();
    var order = createForm.find('input[id="categoryOrder"]').val();

    var paramValue = JSON.stringify({
        Name: name,
        DisplayName: displayName,
        IconPath: iconPath,
        Controller: controller,
        Action: action,
        Role: role,
        Order: order
    });

    $.ajax({
        url: '/Management/CreateCategory',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

                toastr.success('Successfully created the category');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function CreateSubCategory() {
    if (!$('#formCreateSubCategory').valid()) {
        return false;
    }

    var createForm = $('#formCreateSubCategory');

    var categoryId = createForm.find('input[id="subcategoryCategoryId"]').val();
    var name = createForm.find('input[id="subcategoryName"]').val();
    var displayName = createForm.find('input[id="subcategoryDisplayName"]').val();
    var iconPath = createForm.find('input[id="subcategoryIconPath"]').val();
    var action = createForm.find('input[id="subcategoryAction"]').val();
    var role = createForm.find('select[id="subcategoryRole"]').val();
    var order = createForm.find('input[id="subcategoryOrder"]').val();

    var paramValue = JSON.stringify({
        CategoryId: categoryId,
        Name: name,
        DisplayName: displayName,
        IconPath: iconPath,
        Action: action,
        Role: role,
        Order: order
    });

    $.ajax({
        url: '/Management/CreateSubCategory',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

                toastr.success('Successfully created the subCategory');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}