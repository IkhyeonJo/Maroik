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
                let imgNode = document.createElement('img');
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

function CreateCategory() {

    alert('CreateCategory');
    return false;

    if (!$('#formCreateCategory').valid()) {
        return false;
    }

    let createForm = $('#formCreateCategory');
    let name = createForm.find('input[id="createCategoryName"]').val();
    let displayName = createForm.find('input[id="createCategoryDisplayName"]').val();
    let iconPath = createForm.find('input[id="createCategoryIconPath"]').val();
    let controller = createForm.find('input[id="createCategoryController"]').val();
    let action = createForm.find('input[id="createCategoryAction"]').val();
    let role = createForm.find('select[id="createCategoryRole"]').val();
    let order = createForm.find('input[id="createCategoryOrder"]').val();

    let paramValue = JSON.stringify({
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
                $('#createEventTaskDialogModal').modal('hide'); // hide Modal

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

function CreateSubCategory() {

    alert('CreateSubCategory');
    return false;

    if (!$('#formCreateSubCategory').valid()) {
        return false;
    }

    let createForm = $('#formCreateSubCategory');

    let categoryId = createForm.find('input[id="createSubcategoryCategoryId"]').val();
    let name = createForm.find('input[id="createSubcategoryName"]').val();
    let displayName = createForm.find('input[id="createSubcategoryDisplayName"]').val();
    let iconPath = createForm.find('input[id="createSubcategoryIconPath"]').val();
    let action = createForm.find('input[id="createSubcategoryAction"]').val();
    let role = createForm.find('select[id="createSubcategoryRole"]').val();
    let order = createForm.find('input[id="createSubcategoryOrder"]').val();

    let paramValue = JSON.stringify({
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
                $('#createEventTaskDialogModal').modal('hide'); // hide Modal

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

$('#formCreateCategory').off('submit').on('submit', function () {
    return CreateCategory();
});

$('#formCreateSubCategory').off('submit').on('submit', function () {
    return CreateSubCategory();
});

$('#createEventAllDay').off('click').on('click', function () {
    if ($(this).is(':checked')) {
        $("#divCreateEventAllDayUnchecked").hide();
        $("#divCreateEventAllDayChecked").hide();
        $("#divCreateEventAllDayChecked").show();
    }
    else {
        $("#divCreateEventAllDayUnchecked").hide();
        $("#divCreateEventAllDayChecked").hide();
        $("#divCreateEventAllDayUnchecked").show();
    }
});

$('#createEventNotification').off('click').on('click', function () {
    $("<div><select class='createEventSelNotification' style='max-width:90%;'><option value='FiveMinutesBefore' select>5 minutes before</option><option value='TenMinutesBefore'>10 minutes before</option></select><a class='hover aCalendarDeleteNotification' href='#'>&nbsp;<i class='fa fa-trash' aria-hidden='true'></i></a><br /><br /></div>").insertBefore(this);
});

$(document).off('click').on('click', '.aCalendarDeleteNotification', function () {
    $(this).parent().remove();
});