// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $("#Tabs").tabs();
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

    $('#my-grid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#my-grid > table > tbody  > tr').each(function (index, tr) {
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


$(document).on("dblclick", "#my-grid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#my-grid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            console.log("SelectedRow Id:" + $(this).attr("data-id"));
            //pop Modal or something... 
        }
    });
    //console.log('dblclick-end');
});
$('#GridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("WholeSearch", this.value);

    grid.reload();
});

function CategoryExportExcel() {
    var form = document.createElement("form");
    var element1 = document.createElement("input");
    var element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/Management/CategoryExportExcel";

    element1.name = "__RequestVerificationToken";
    element1.value = $('input[name="__RequestVerificationToken"]').val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "listOfCategories";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}

function CreateCategory() {
    var createForm = $('#CategoryCreate');
    //var id = createForm.find('input[id="Id"]').val();
    var role = createForm.find('select[id="Role"]').val();
    alert(role);

    $('#CreateDialogModal').modal('hide'); // hide Modal

    //doubtful code (not yet Insert function created...!)
    const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
    grid.reload(); //reload grid
    //doubtful code (not yet Insert function created...!)

    //var form = document.createElement("form");
    //var element1 = document.createElement("input");
    //var element2 = document.createElement("input");

    //form.method = "POST";
    //form.action = "/Management/CategoryExportExcel";

    //element1.name = "__RequestVerificationToken";
    //element1.value = $('input[name="__RequestVerificationToken"]').val();
    //form.appendChild(element1);

    //element2.name = "fileName";
    //element2.value = "listOfCategories";
    //form.appendChild(element2);

    //document.body.appendChild(form);

    //form.submit();
}

function CreateSubCategory() {
    var createForm = $('#SubCategoryCreate');
    //var id = createForm.find('input[id="Id"]').val();
    var role = createForm.find('select[id="Role"]').val();
    alert(role);

    $('#CreateDialogModal').modal('hide'); // hide Modal

    //doubtful code (not yet Insert function created...!)
    const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
    grid.reload(); //reload grid
    //doubtful code (not yet Insert function created...!)

    //var form = document.createElement("form");
    //var element1 = document.createElement("input");
    //var element2 = document.createElement("input");

    //form.method = "POST";
    //form.action = "/Management/CategoryExportExcel";

    //element1.name = "__RequestVerificationToken";
    //element1.value = $('input[name="__RequestVerificationToken"]').val();
    //form.appendChild(element1);

    //element2.name = "fileName";
    //element2.value = "listOfCategories";
    //form.appendChild(element2);

    //document.body.appendChild(form);

    //form.submit();
}