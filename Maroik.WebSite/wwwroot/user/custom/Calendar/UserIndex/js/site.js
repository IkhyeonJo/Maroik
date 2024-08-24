function CreateUploadImageFile(file) {

    formData = new FormData();
    formData.append("summernoteImageFile", file);

    $.ajax({
        url: "/Calendar/UploadImageFile",
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
                $('#createCalendarEventDescription').summernote('insertNode', imgNode);
            }
            else {
                alert(data.errorMessage);
            }
        }
    });
}

function EditUploadImageFile(file) {

    formData = new FormData();
    formData.append("summernoteImageFile", file);

    $.ajax({
        url: "/Calendar/UploadImageFile",
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
                $('#editCalendarEventDescription').summernote('insertNode', imgNode);
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
    let htmlColorCode = createForm.find('input[id="createCalendarHtmlColorCode"]').val();
    let timeZoneIanaId = createForm.find('select[id="createCalendarTimeZone"]').val();

    let calendarsArray = [
        { Name: name, Description: description, HtmlColorCode: htmlColorCode, TimeZoneIanaId: timeZoneIanaId }
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
                $('#createCalendarDialogModal').modal('hide');

                $('#myCalendars').append(`<label id="lblCalendar${data.calendar.id}" style="position: relative; border:1px solid #ccc; padding:10px; margin:0 0 10px; display:block"><input type="checkbox" style="accent-color:${data.calendar.htmlColorCode}" checked /><label style="padding-left:0.5em;"> ${data.calendar.name}</label><i id="${data.calendar.id}" class="edit-calendar fas fa-edit" style="position: absolute; right: 40px; top: 50%; transform: translateY(-50%); cursor: pointer;"></i><i id="${data.calendar.id}" class="delete-calendar fas fa-trash" style="position: absolute; right: 15px; top: 50%; transform: translateY(-50%); cursor: pointer;"></i></label>`);

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

$('#aCreateCalendarEvent').off('click').on('click', function () {

    $.ajax({
        url: '/Calendar/GetCalendars',
        method: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        async: false,
        //cache: false,
        success: function (response) {
            if (response.result) {
                $('#createCalendarEventMyCalendar').empty();

                $.each(response.calendars, function (index, calendar) {
                    $('#createCalendarEventMyCalendar').append($('<option>', {
                        value: calendar.id,
                        text: calendar.name
                    }));
                });
            }
        }
    });

    $('#createCalendarEventTaskDialogModal').modal('show');

    $('#createCalendarEventAllDayUncheckedStartDate').val(moment().format("YYYY-MM-DD"));
    $('#createCalendarEventAllDayCheckedStartDate').val(moment().format("YYYY-MM-DD"));

    $('#createCalendarEventAllDayUncheckedEndDate').val(moment().format("YYYY-MM-DD"));
    $('#createCalendarEventAllDayCheckedEndDate').val(moment().format("YYYY-MM-DD"));
});

$('#createCalendarEventAllDay').off('click').on('click', function () {
    if ($(this).is(':checked')) {
        $("#divCreateEventAllDayUnchecked").hide();
        $("#divCreateEventAllDayChecked").hide();
        $("#divCreateEventAllDayChecked").show();

        $("#divCreateEventNotificationAllDayUnchecked").hide();
        $("#divCreateEventNotificationAllDayChecked").hide();
        $("#divCreateEventNotificationAllDayChecked").show();
    }
    else {
        $("#divCreateEventAllDayUnchecked").hide();
        $("#divCreateEventAllDayChecked").hide();
        $("#divCreateEventAllDayUnchecked").show();

        $("#divCreateEventNotificationAllDayUnchecked").hide();
        $("#divCreateEventNotificationAllDayChecked").hide();
        $("#divCreateEventNotificationAllDayUnchecked").show();
    }
});

$('#editCalendarEventAllDay').off('click').on('click', function () {
    if ($(this).is(':checked')) {
        $("#divEditEventAllDayUnchecked").hide();
        $("#divEditEventAllDayChecked").hide();
        $("#divEditEventAllDayChecked").show();

        $("#divEditEventNotificationAllDayUnchecked").hide();
        $("#divEditEventNotificationAllDayChecked").hide();
        $("#divEditEventNotificationAllDayChecked").show();
    }
    else {
        $("#divEditEventAllDayUnchecked").hide();
        $("#divEditEventAllDayChecked").hide();
        $("#divEditEventAllDayUnchecked").show();

        $("#divEditEventNotificationAllDayUnchecked").hide();
        $("#divEditEventNotificationAllDayChecked").hide();
        $("#divEditEventNotificationAllDayUnchecked").show();
    }
});

$('#createCalendarEventNotificationAllDayChecked').off('click').on('click', function () {
    const timeIntervals = Array.from({ length: 96 }, (_, i) => {
        const hours = String(Math.floor(i / 4)).padStart(2, '0');
        const minutes = String((i % 4) * 15).padStart(2, '0');
        return `${hours}:${minutes}`;
    });

    const localizer = {
        Email: "Email",
        Notification: "Notification",
        days: "days",
        weeks: "weeks",
        "before at": "before at"
    };

    const htmlString = String.raw`
    <div class="divCreateEventNotificationAllDayCheckedRow">
        <select class='form-control-sm createCalendarEventSelNotificationMethodAllDayChecked' style='width:20%;text-overflow:ellipsis;'>
            <option value='Email'>${localizer.Email}</option>
            <option value='Notification'>${localizer.Notification}</option>
        </select>

        <input type='number' class='form-control-sm createCalendarEventSelNotificationNumberAllDayChecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="28">

        <select class='form-control-sm createCalendarEventSelNotificationTimeTypeAllDayChecked' style='width:15%;text-overflow:ellipsis;'>
            <option value='Days' selected>${localizer.days}</option>
            <option value='Weeks'>${localizer.weeks}</option>
        </select>
        <label style="padding-left:5px;padding-right:5px;">${localizer["before at"]}</label>
        <select class='form-control-sm createCalendarEventSelNotificationTimeAllDayChecked' style='width:11%;text-overflow:ellipsis;'>
            ${timeIntervals.map(time => time === "09:00" ? `<option value="${time}" selected>${time}</option>` : `<option value="${time}">${time}</option>`).join('')}
        </select>
        <a class='hover aCreateCalendarDeleteNotificationAllDayChecked' href='#' style='width:10%;'>
            <i class='fa fa-trash' aria-hidden='true' style='margin-left: 7px;'></i>
        </a>

        <br />
        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
    </div>
`;

    $(htmlString).insertBefore(this);
});

$('#createCalendarEventNotificationAllDayUnchecked').off('click').on('click', function () {
    const localizer = {
        Email: "Email",
        Notification: "Notification",
        minutes: "minutes",
        hours: "hours",
        days: "days",
        weeks: "weeks"
    };

    const htmlString = String.raw`
    <div class="divCreateEventNotificationAllDayUncheckedRow" style="padding-bottom:5px;">
        <select class='form-control-sm createCalendarEventSelNotificationMethodAllDayUnchecked' style='width:20%;text-overflow:ellipsis;'>
            <option value='Email'>${localizer.Email}</option>
            <option value='Notification'>${localizer.Notification}</option>
        </select>

        <input type='number' class='form-control-sm createCalendarEventSelNotificationNumberAllDayUnchecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="40320">

        <select class='form-control-sm createCalendarEventSelNotificationTimeTypeAllDayUnchecked' style='width:15%;text-overflow:ellipsis;'>
            <option value='Minutes' selected>${localizer.minutes}</option>
            <option value='Hours'>${localizer.hours}</option>
            <option value='Days'>${localizer.days}</option>
            <option value='Weeks'>${localizer.weeks}</option>
        </select>
        <a class='hover aCreateCalendarDeleteNotificationAllDayUnchecked' href='#' style='width:10%;'>
            <i class='fa fa-trash' aria-hidden='true' style='margin-left: 7px;'></i>
        </a>

        <br />
        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
    </div>
`;

    $(htmlString).insertBefore(this);
});

$('#editCalendarEventNotificationAllDayChecked').off('click').on('click', function () {
    const timeIntervals = Array.from({ length: 96 }, (_, i) => {
        const hours = String(Math.floor(i / 4)).padStart(2, '0');
        const minutes = String((i % 4) * 15).padStart(2, '0');
        return `${hours}:${minutes}`;
    });

    const localizer = {
        Email: "Email",
        Notification: "Notification",
        days: "days",
        weeks: "weeks",
        "before at": "before at"
    };

    const htmlString = String.raw`
    <div class="divEditEventNotificationAllDayCheckedRow">
        <select class='form-control-sm editCalendarEventSelNotificationMethodAllDayChecked' style='width:20%;text-overflow:ellipsis;'>
            <option value='Email'>${localizer.Email}</option>
            <option value='Notification'>${localizer.Notification}</option>
        </select>

        <input type='number' class='form-control-sm editCalendarEventSelNotificationNumberAllDayChecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="28">

        <select class='form-control-sm editCalendarEventSelNotificationTimeTypeAllDayChecked' style='width:15%;text-overflow:ellipsis;'>
            <option value='Days' selected>${localizer.days}</option>
            <option value='Weeks'>${localizer.weeks}</option>
        </select>
        <label style="padding-left:5px;padding-right:5px;">${localizer["before at"]}</label>
        <select class='form-control-sm editCalendarEventSelNotificationTimeAllDayChecked' style='width:11%;text-overflow:ellipsis;'>
            ${timeIntervals.map(time => time === "09:00" ? `<option value="${time}" selected>${time}</option>` : `<option value="${time}">${time}</option>`).join('')}
        </select>
        <a class='hover aEditCalendarDeleteNotificationAllDayChecked' href='#' style='width:10%;'>
            <i class='fa fa-trash' aria-hidden='true' style='margin-left: 7px;'></i>
        </a>

        <br />
        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
    </div>
`;

    $(htmlString).insertBefore(this);
});

$('#editCalendarEventNotificationAllDayUnchecked').off('click').on('click', function () {
    const localizer = {
        Email: "Email",
        Notification: "Notification",
        minutes: "minutes",
        hours: "hours",
        days: "days",
        weeks: "weeks"
    };

    const htmlString = String.raw`
    <div class="divEditEventNotificationAllDayUncheckedRow" style="padding-bottom:5px;">
        <select class='form-control-sm editCalendarEventSelNotificationMethodAllDayUnchecked' style='width:20%;text-overflow:ellipsis;'>
            <option value='Email'>${localizer.Email}</option>
            <option value='Notification'>${localizer.Notification}</option>
        </select>

        <input type='number' class='form-control-sm editCalendarEventSelNotificationNumberAllDayUnchecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="40320">

        <select class='form-control-sm editCalendarEventSelNotificationTimeTypeAllDayUnchecked' style='width:15%;text-overflow:ellipsis;'>
            <option value='Minutes' selected>${localizer.minutes}</option>
            <option value='Hours'>${localizer.hours}</option>
            <option value='Days'>${localizer.days}</option>
            <option value='Weeks'>${localizer.weeks}</option>
        </select>
        <a class='hover aEditCalendarDeleteNotificationAllDayUnchecked' href='#' style='width:10%;'>
            <i class='fa fa-trash' aria-hidden='true' style='margin-left: 7px;'></i>
        </a>

        <br />
        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
    </div>
`;

    $(htmlString).insertBefore(this);
});

$(document).on('click', '.aCreateCalendarDeleteNotificationAllDayChecked', function () {
    $(this).parent().remove();
});

$(document).on('click', '.aCreateCalendarDeleteNotificationAllDayUnchecked', function () {
    $(this).parent().remove();
});

$(document).on('click', '.aEditCalendarDeleteNotificationAllDayChecked', function () {
    $(this).parent().remove();
});

$(document).on('click', '.aEditCalendarDeleteNotificationAllDayUnchecked', function () {
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

$(document).on('click', '.edit-calendar', function (e) {
    e.preventDefault();
    e.stopPropagation();

    let selectedCalendarId = $(this).parent().attr('id').replace("lblCalendar", "");

    $.ajax({
        url: '/Calendar/IsCalendarExists' + '?id=' + selectedCalendarId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                let formEditCalendar = $('#formEditCalendar');

                formEditCalendar.find('input[id="editCalendarId"]').val(data.calendar.id);
                formEditCalendar.find('input[id="editCalendarName"]').val(data.calendar.name);
                formEditCalendar.find('textarea[id="editCalendarDescription"]').val(data.calendar.description);
                formEditCalendar.find('input[id="editCalendarHtmlColorCode"]').val(data.calendar.htmlColorCode);
                formEditCalendar.find('select[id="editCalendarTimeZone"]').val(data.calendar.timeZoneIanaId).change();

                $('#editCalendarDialogModal').modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $('#editCalendarDialogModal').modal('toggle');
                $('#editCalendarDialogModal').modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
});

$(document).on('mouseenter', '.edit-calendar', function () {
    $(this).css('color', 'blue');
});

$(document).on('mouseleave', '.edit-calendar', function () {
    $(this).css('color', '');
});

$(document).on('click', '.delete-calendar', function (e) {
    e.preventDefault();
    e.stopPropagation();

    let calendarId = $(this).parent().attr('id').replace("lblCalendar", "");
    $('#confirmDeleteCalendarDialogModal').data('calendar-id', calendarId);

    $('#confirmDeleteCalendarDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteCalendarDialogModal').modal('toggle');
    $('#confirmDeleteCalendarDialogModal').modal('show');
});

$(document).on('mouseenter', '.delete-calendar', function () {
    $(this).css('color', 'blue');
});

$(document).on('mouseleave', '.delete-calendar', function () {
    $(this).css('color', '');
});