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

function CreateCalendar() {

    if (!$('#formCreateCalendar').valid()) {
        return false;
    }

    let createForm = $('#formCreateCalendar');
    let name = createForm.find('input[id="createCalendarName"]').val();
    let description = createForm.find('textarea[id="createCalendarDescription"]').val();
    let timeZoneIanaId = createForm.find('select[id="createCalendarTimeZone"]').val();

    let calendarsArray = [
        { Name: name, Description: description, TimeZoneIanaId: timeZoneIanaId }
    ];

    let paramValue = {
        Calendars: []
    };

    for (let i = 0; i < calendarsArray.length; i++) {
        paramValue.Calendars.push(calendarsArray[i]);
    }

    $.ajax({
        url: '/Calendar/CreateCalendar',
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: JSON.stringify(paramValue),
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createCalendarDialogModal').modal('hide'); // hide Modal

                //const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                //grid.reload(); //reload grid

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

$('#formCreateCalendar').off('submit').on('submit', function () {
    return CreateCalendar();
});

$('#formCreateSubCategory').off('submit').on('submit', function () {
    return CreateSubCategory();
});

$('#aCreateCalendarEvent').off('click').on('click', function () {
    $('#createEventTaskDialogModal').modal('show');

    $('#createEventAllDayUncheckedStartDate').val(moment().format("YYYY-MM-DD"));
    $('#createEventAllDayCheckedStartDate').val(moment().format("YYYY-MM-DD"));

    $('#createEventAllDayUncheckedEndDate').val(moment().format("YYYY-MM-DD"));
    $('#createEventAllDayCheckedEndDate').val(moment().format("YYYY-MM-DD"));
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
    $("<div><select class='form-control-sm createEventSelNotification' style='width:90%;text-overflow:ellipsis;'><option value='FiveMinutesBefore' select>5 minutes before</option><option value='TenMinutesBefore'>10 minutes before</option></select><a class='hover aCalendarDeleteNotification' href='#' style='width:10%;'><i class='fa fa-trash' aria-hidden='true' style='margin-left: 7px;'></i></a><br /><br /></div>").insertBefore(this);
});

$(document).on('click', '.aCalendarDeleteNotification', function () {
    $(this).parent().remove();
});

$('#dropdown-icon').click(function () {
    $('#dropdown-content').toggle();
});

$(window).click(function (event) {
    if (!$(event.target).closest('#dropdown-icon, #dropdown-content').length) {
        $('#dropdown-content').hide();
    }
});

$('#dropdown-content a').click(function () {
    $('#dropdown-content').hide();
});

$('#aCreateCalendar').off('click').on('click', function () {
    $('#createCalendarDialogModal').modal('show');
});

$('.edit-calendar').off('click').on('click', function (e) {
    e.preventDefault();
    e.stopPropagation();

    let id = $(this).attr('id');
    alert(`id: ${id} edit-calendar!!`);
});