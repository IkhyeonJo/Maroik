const $viewCalendarEventTaskTabs = $("#viewCalendarEventTaskTabs");
const $otherCalendarEventPopup = $('#otherCalendarEventPopup');
const $otherCalendarEventPopupTitle = $('#otherCalendarEventPopupTitle');
const $divOtherCalendarEventPopupAllDayChecked = $('#divOtherCalendarEventPopupAllDayChecked');
const $divOtherCalendarEventPopupAllDayUnchecked = $('#divOtherCalendarEventPopupAllDayUnchecked');
const $otherCalendarEventPopupStartAllDayChecked = $('#otherCalendarEventPopupStartAllDayChecked');
const $otherCalendarEventPopupEndAllDayChecked = $('#otherCalendarEventPopupEndAllDayChecked');
const $otherCalendarEventPopupStartAllDayUnchecked = $('#otherCalendarEventPopupStartAllDayUnchecked');
const $otherCalendarEventPopupStartTimeZoneAllDayUnchecked = $('#otherCalendarEventPopupStartTimeZoneAllDayUnchecked');
const $otherCalendarEventPopupEndAllDayUnchecked = $('#otherCalendarEventPopupEndAllDayUnchecked');
const $otherCalendarEventPopupEndTimeZoneAllDayUnchecked = $('#otherCalendarEventPopupEndTimeZoneAllDayUnchecked');
const $viewOtherCalendarEventPopup = $('#viewOtherCalendarEventPopup');
const $closeOtherCalendarEventPopup = $('#closeOtherCalendarEventPopup');
const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
const $viewCalendarEventId = $('#viewCalendarEventId');
const $viewCalendarEventName = $('#viewCalendarEventName');
const $viewCalendarEventAllDay = $('#viewCalendarEventAllDay');
const $divViewEventAllDayChecked = $('#divViewEventAllDayChecked');
const $divViewEventAllDayUnchecked = $('#divViewEventAllDayUnchecked');
const $viewCalendarEventAllDayCheckedStartDate = $('#viewCalendarEventAllDayCheckedStartDate');
const $viewCalendarEventAllDayCheckedEndDate = $('#viewCalendarEventAllDayCheckedEndDate');
const $viewCalendarEventAllDayUncheckedStartDate = $('#viewCalendarEventAllDayUncheckedStartDate');
const $viewCalendarEventAllDayUncheckedStartTime = $('#viewCalendarEventAllDayUncheckedStartTime');
const $viewCalendarEventAllDayUncheckedStartTimeZone = $('#viewCalendarEventAllDayUncheckedStartTimeZone');
const $viewCalendarEventAllDayUncheckedEndDate = $('#viewCalendarEventAllDayUncheckedEndDate');
const $viewCalendarEventAllDayUncheckedEndTime = $('#viewCalendarEventAllDayUncheckedEndTime');
const $viewCalendarEventAllDayUncheckedEndTimeZone = $('#viewCalendarEventAllDayUncheckedEndTimeZone');
const $viewCalendarEventLocation = $('#viewCalendarEventLocation');
const $viewCalendarEventDescription = $('#viewCalendarEventDescription');
const $divViewCalendarEventAttachedFile = $('#divViewCalendarEventAttachedFile');
const $aViewCalendarEventAttachedFile = $("#aViewCalendarEventAttachedFile");
const $spanViewCalendarEventAttachedFile = $("#spanViewCalendarEventAttachedFile");
const $viewCalendarEventMyCalendar = $('#viewCalendarEventMyCalendar');
const $viewCalendarEventStatus = $('#viewCalendarEventStatus');
const $divViewEventNotificationAllDayChecked = $('#divViewEventNotificationAllDayChecked');
const $divViewEventNotificationAllDayUnchecked = $('#divViewEventNotificationAllDayUnchecked');
const $viewCalendarEventNotificationAllDayChecked = $('#viewCalendarEventNotificationAllDayChecked');
const $viewCalendarEventNotificationAllDayUnchecked = $('#viewCalendarEventNotificationAllDayUnchecked');
const $viewCalendarEventTaskDialogModal = $('#viewCalendarEventTaskDialogModal');
const $otherCalendarEventOutputViewModels = $('#otherCalendarEventOutputViewModels');
const $otherCalendars = $('#otherCalendars');

function base64ToBlob(base64, mime) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    return new Blob([byteArray], { type: mime });
}

const localizer = {
    Email: $('#LocalizerEmail').val(),
    Notification: $('#LocalizerNotification').val(),
    Minutes: $('#LocalizerMinutes').val(),
    Hours: $('#LocalizerHours').val(),
    Days: $('#LocalizerDays').val(),
    Weeks: $('#LocalizerWeeks').val(),
    BeforeAt: $('#LocalizerBeforeAt').val(),
    PrevText: $('#LocalizerPrevText').val(),
    NextText: $('#LocalizerNextText').val(),
    January: $('#LocalizerJanuary').val(),
    February: $('#LocalizerFebruary').val(),
    March: $('#LocalizerMarch').val(),
    April: $('#LocalizerApril').val(),
    May: $('#LocalizerMay').val(),
    June: $('#LocalizerJune').val(),
    July: $('#LocalizerJuly').val(),
    August: $('#LocalizerAugust').val(),
    September: $('#LocalizerSeptember').val(),
    October: $('#LocalizerOctober').val(),
    November: $('#LocalizerNovember').val(),
    December: $('#LocalizerDecember').val(),
    Jan: $('#LocalizerJan').val(),
    Feb: $('#LocalizerFeb').val(),
    Mar: $('#LocalizerMar').val(),
    Apr: $('#LocalizerApr').val(),
    Jun: $('#LocalizerJun').val(),
    Jul: $('#LocalizerJul').val(),
    Aug: $('#LocalizerAug').val(),
    Sep: $('#LocalizerSep').val(),
    Oct: $('#LocalizerOct').val(),
    Nov: $('#LocalizerNov').val(),
    Dec: $('#LocalizerDec').val(),
    Sunday: $('#LocalizerSunday').val(),
    Monday: $('#LocalizerMonday').val(),
    Tuesday: $('#LocalizerTuesday').val(),
    Wednesday: $('#LocalizerWednesday').val(),
    Thursday: $('#LocalizerThursday').val(),
    Friday: $('#LocalizerFriday').val(),
    Saturday: $('#LocalizerSaturday').val(),
    Sun: $('#LocalizerSun').val(),
    Mon: $('#LocalizerMon').val(),
    Tue: $('#LocalizerTue').val(),
    Wed: $('#LocalizerWed').val(),
    Thu: $('#LocalizerThu').val(),
    Fri: $('#LocalizerFri').val(),
    Sat: $('#LocalizerSat').val(),
    Su: $('#LocalizerSu').val(),
    Mo: $('#LocalizerMo').val(),
    Tu: $('#LocalizerTu').val(),
    We: $('#LocalizerWe').val(),
    Th: $('#LocalizerTh').val(),
    Fr: $('#LocalizerFr').val(),
    Sa: $('#LocalizerSa').val(),
    YearSuffix: $('#LocalizerYearSuffix').val(),
    IETFLanguageTag: $('#LocalizerIETFLanguageTag').val(),
    Prev: $('#LocalizerPrev').val(),
    Next: $('#LocalizerNext').val(),
    PrevYear: $('#LocalizerPrevYear').val(),
    NextYear: $('#LocalizerNextYear').val(),
    Today: $('#LocalizerToday').val(),
    Month: $('#LocalizerMonth').val(),
    Week: $('#LocalizerWeek').val(),
    Day: $('#LocalizerDay').val(),
    List: $('#LocalizerList').val(),
    DayGridMonth: $('#LocalizerDayGridMonth').val(),
    DayGridWeek: $('#LocalizerDayGridWeek').val(),
    DayGridDay: $('#LocalizerDayGridDay').val(),
    TimeGridWeek: $('#LocalizerTimeGridWeek').val(),
    TimeGridDay: $('#LocalizerTimeGridDay').val(),
    ListYear: $('#LocalizerListYear').val(),
    ListMonth: $('#LocalizerListMonth').val(),
    ListWeek: $('#LocalizerListWeek').val(),
    ListDay: $('#LocalizerListDay').val(),
    ConfirmDelete: $('#LocalizerConfirmDelete').val(),
    ThisFieldRequired: $('#LocalizerThisFieldRequired').val(),
    ErrorInvalidNumber: $('#LocalizerErrorInvalidNumber').val(),
    ErrorRangeMinute: $('#LocalizerErrorRangeMinute').val(),
    ErrorRangeHour: $('#LocalizerErrorRangeHour').val(),
    ErrorRangeDay: $('#LocalizerErrorRangeDay').val(),
    ErrorRangeWeek: $('#LocalizerErrorRangeWeek').val()
};

let calendar;

$(function () {
    $viewCalendarEventTaskTabs.tabs();

    calendar = new FullCalendar.Calendar(document.getElementById('calendar'), {
        locale: localizer.IETFLanguageTag,
        headerToolbar: {
            left: 'prevYear,prev,next,nextYear today',
            center: 'title',
            right: 'dayGridMonth'
        },
        buttonText: {
            today: localizer.Today,
            dayGridMonth: localizer.Month
        },
        themeSystem: 'bootstrap',
        navLinks: false,
        editable: false,
        selectable: true,
        selectMirror: true,
        eventClick: function (arg) {
            if (arg.event.extendedProps.calendarType === 'Other') {
                let eventEl = $(arg.el);
                let offset = eventEl.offset();
                let popup = $otherCalendarEventPopup;
                let currentEventId = popup.data('event-id');

                if (popup.is(':visible') && currentEventId === arg.event.id) {
                    popup.hide();
                    return;
                }

                $otherCalendarEventPopupTitle.text(arg.event.title);
                $divOtherCalendarEventPopupAllDayChecked.hide();
                $divOtherCalendarEventPopupAllDayUnchecked.hide();

                if (arg.event.allDay === true) {
                    $otherCalendarEventPopupStartAllDayChecked.text(arg.event.extendedProps.displayStartDate);
                    $otherCalendarEventPopupEndAllDayChecked.text(arg.event.extendedProps.displayEndDate);
                    $divOtherCalendarEventPopupAllDayChecked.show();
                } else {
                    $otherCalendarEventPopupStartAllDayUnchecked.text(arg.event.extendedProps.displayStartDate);
                    $otherCalendarEventPopupStartTimeZoneAllDayUnchecked.text(`(${arg.event.extendedProps.displayStartDateTimeZone})`);
                    $otherCalendarEventPopupEndAllDayUnchecked.text(arg.event.extendedProps.displayEndDate);
                    $otherCalendarEventPopupEndTimeZoneAllDayUnchecked.text(`(${arg.event.extendedProps.displayEndDateTimeZone})`);
                    $divOtherCalendarEventPopupAllDayUnchecked.show();
                }

                popup.css({
                    top: offset.top + eventEl.outerHeight(),
                    left: offset.left + eventEl.outerWidth(),
                    display: 'block'
                }).data('event-id', arg.event.id);

                $viewOtherCalendarEventPopup.focus();

                $(document).off('click.AnonymousIndex').on('click.AnonymousIndex', function (e) {
                    if (!$(e.target).closest($otherCalendarEventPopup).length && !$(e.target).closest('.fc-event').length) {
                        popup.hide();
                    }
                });

                $closeOtherCalendarEventPopup.off('click').on('click', function () {
                    popup.hide();
                });

                $viewOtherCalendarEventPopup.off('click').on('click', function () {
                    $.ajax({
                        url: '/Calendar/IsOtherCalendarEventExists' + '?id=' + popup.data('event-id'),
                        type: 'POST',
                        headers: { 'RequestVerificationToken': $__RequestVerificationToken.val() },
                        dataType: 'json',
                        data: null,
                        contentType: 'application/json; charset=utf-8',
                        success: function (data) {
                            if (data.result) {
                                $viewCalendarEventId.val(data.calendarEvent.id);
                                $viewCalendarEventName.val(data.calendarEvent.title);
                                $viewCalendarEventAllDay.val(data.calendarEvent.allDay);

                                $divViewEventAllDayChecked.hide();
                                $divViewEventAllDayUnchecked.hide();

                                if (data.calendarEvent.allDay === true) {
                                    $divViewEventAllDayChecked.show();
                                    $viewCalendarEventAllDay.prop('checked', true);
                                    $viewCalendarEventAllDayCheckedStartDate.val(data.calendarEvent.displayStartDate);
                                    $viewCalendarEventAllDayCheckedEndDate.val(data.calendarEvent.displayEndDate);

                                }
                                else {
                                    $divViewEventAllDayUnchecked.show();

                                    $viewCalendarEventAllDay.prop('checked', false);

                                    $viewCalendarEventAllDayUncheckedStartDate.val(data.calendarEvent.displayStartDate.split(' ')[0]);
                                    $viewCalendarEventAllDayUncheckedStartTime.val(data.calendarEvent.displayStartDate.split(' ')[1].substring(0, 5));
                                    $viewCalendarEventAllDayUncheckedStartTimeZone.val(data.calendarEvent.startDateTimeZoneIanaId);

                                    $viewCalendarEventAllDayUncheckedEndDate.val(data.calendarEvent.displayEndDate.split(' ')[0]);
                                    $viewCalendarEventAllDayUncheckedEndTime.val(data.calendarEvent.displayEndDate.split(' ')[1].substring(0, 5));
                                    $viewCalendarEventAllDayUncheckedEndTimeZone.val(data.calendarEvent.endDateTimeZoneIanaId);
                                }

                                $viewCalendarEventLocation.val(data.calendarEvent.location);

                                const parser = new DOMParser();
                                const htmlDoc = parser.parseFromString(data.calendarEvent.description, 'text/html');

                                const imgTags = htmlDoc.querySelectorAll('img[data-file]');

                                imgTags.forEach(function (imgTag) {
                                    const base64Data = imgTag.getAttribute('data-file');
                                    const contentType = imgTag.getAttribute('data-contenttype');

                                    if (base64Data && contentType) {
                                        const blob = base64ToBlob(base64Data, contentType);
                                        const imgURL = URL.createObjectURL(blob);

                                        imgTag.src = imgURL;

                                        imgTag.onload = function () {
                                            URL.revokeObjectURL(imgURL);
                                        };
                                        imgTag.onerror = function () {
                                            URL.revokeObjectURL(imgURL);
                                        };

                                        imgTag.removeAttribute('data-file');
                                        imgTag.removeAttribute('data-contenttype');
                                    }
                                });

                                const updatedHtml = htmlDoc.body.innerHTML;

                                $viewCalendarEventDescription.summernote('code', updatedHtml);
                                $viewCalendarEventDescription.summernote('disable');

                                $divViewCalendarEventAttachedFile.hide();

                                if (data.calendarEvent.calendarEventAttachedFile !== null) {
                                    $divViewCalendarEventAttachedFile.show();

                                    $aViewCalendarEventAttachedFile.attr("href", '#');
                                    $aViewCalendarEventAttachedFile.attr("data-file", data.calendarEvent.calendarEventAttachedFileBase64Data);
                                    $aViewCalendarEventAttachedFile.attr("data-contenttype", data.calendarEvent.calendarEventAttachedFileContentType);
                                    $aViewCalendarEventAttachedFile.attr("data-name", `${data.calendarEvent.calendarEventAttachedFile.name}${data.calendarEvent.calendarEventAttachedFile.extension}`);

                                    $aViewCalendarEventAttachedFile.text(`${data.calendarEvent.calendarEventAttachedFile.name}${data.calendarEvent.calendarEventAttachedFile.extension}`);

                                    $spanViewCalendarEventAttachedFile.text(`${Math.round(data.calendarEvent.calendarEventAttachedFile.size / 1024).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",")}KB`);
                                }


                                $.ajax({
                                    url: '/Calendar/GetOtherCalendars',
                                    method: 'POST',
                                    headers: { 'RequestVerificationToken': $__RequestVerificationToken.val() },
                                    dataType: 'json',
                                    data: null,
                                    contentType: 'application/json; charset=utf-8',
                                    async: false,
                                    success: function (response) {
                                        if (response.result) {
                                            $viewCalendarEventMyCalendar.empty();

                                            $.each(response.tempOtherCalendars, function (index, tempOtherCalendar) {
                                                $viewCalendarEventMyCalendar.append($('<option>', {
                                                    value: tempOtherCalendar.id,
                                                    text: tempOtherCalendar.name
                                                }));
                                            });

                                            $viewCalendarEventMyCalendar.val(data.calendarEvent.calendarId);
                                        }
                                    }
                                });

                                $viewCalendarEventStatus.val(data.calendarEvent.status);

                                $divViewEventNotificationAllDayChecked.hide();
                                $divViewEventNotificationAllDayUnchecked.hide();

                                if (data.calendarEvent.allDay === true) {
                                    $divViewEventNotificationAllDayChecked.find('.divViewEventNotificationAllDayCheckedRow').remove();
                                    $divViewEventNotificationAllDayChecked.show();

                                    if (data.calendarEvent.serializedCalendarReminders !== '[]') {
                                        const timeIntervals = Array.from({ length: 96 }, (_, i) => {
                                            const hours = String(Math.floor(i / 4)).padStart(2, '0');
                                            const minutes = String((i % 4) * 15).padStart(2, '0');
                                            return `${hours}:${minutes}`;
                                        });

                                        JSON.parse(data.calendarEvent.serializedCalendarReminders).forEach(reminder => {
                                            if (reminder.Method !== 'Email') {
                                                const selectedMethodOption = `
                                                    <option value='Email' ${reminder.Method === 'Email' ? 'selected' : ''}>${localizer.Email}</option>
                                                    <option value='Notification' ${reminder.Method === 'Notification' ? 'selected' : ''}>${localizer.Notification}</option>
                                                `;

                                                let value = reminder.DaysBeforeEvent !== null ? reminder.DaysBeforeEvent : reminder.WeeksBeforeEvent;
                                                let max = reminder.DaysBeforeEvent !== null ? 28 : 4;
                                                let selectedDays = reminder.DaysBeforeEvent !== null ? 'selected' : '';
                                                let selectedWeeks = reminder.WeeksBeforeEvent !== null ? 'selected' : '';

                                                const selNotificationTimeTypeAllDayChecked = `
                                                <input type='number' class='form-control-sm viewCalendarEventSelNotificationNumberAllDayChecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="${max}" value="${value}" disabled>
                                                <select class='form-control-sm viewCalendarEventSelNotificationTimeTypeAllDayChecked' style='width:15%;text-overflow:ellipsis;' disabled>
                                                    <option value='Days' ${selectedDays}>${localizer.Days}</option>
                                                    <option value='Weeks' ${selectedWeeks}>${localizer.Weeks}</option>
                                                </select>`;

                                                const htmlString = String.raw`
                                                    <div class="divViewEventNotificationAllDayCheckedRow">
                                                        <select class='form-control-sm viewCalendarEventSelNotificationMethodAllDayChecked' style='width:20%;text-overflow:ellipsis;' disabled>
                                                            ${selectedMethodOption}
                                                        </select>

                                                        ${selNotificationTimeTypeAllDayChecked}

                                                        <label style="padding-left:5px;padding-right:5px;">${localizer.BeforeAt}</label>
                                                        <select class='form-control-sm viewCalendarEventSelNotificationTimeAllDayChecked' style='width:11%;text-overflow:ellipsis;' disabled>
                                                            ${timeIntervals.map(time => time === `${reminder.TimesBeforeEvent.substring(0, 5)}` ? `<option value="${time}" selected>${time}</option>` : `<option value="${time}">${time}</option>`).join('')}
                                                        </select>

                                                        <br />
                                                        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
                                                    </div>
                                                `;

                                                $(htmlString).insertBefore($viewCalendarEventNotificationAllDayChecked);
                                            }
                                        });
                                    }
                                }
                                else {
                                    $divViewEventNotificationAllDayUnchecked.find('.divViewEventNotificationAllDayUncheckedRow').remove();
                                    $divViewEventNotificationAllDayUnchecked.show();

                                    if (data.calendarEvent.serializedCalendarReminders !== '[]') {

                                        JSON.parse(data.calendarEvent.serializedCalendarReminders).forEach(reminder => {
                                            if (reminder.Method !== 'Email') {
                                                const selectedMethodOption = `
                                                    <option value='Email' ${reminder.Method === 'Email' ? 'selected' : ''}>${localizer.Email}</option>
                                                    <option value='Notification' ${reminder.Method === 'Notification' ? 'selected' : ''}>${localizer.Notification}</option>
                                                `;

                                                let value = reminder.MinutesBeforeEvent !== null ? reminder.MinutesBeforeEvent : reminder.HoursBeforeEvent !== null ? reminder.HoursBeforeEvent : reminder.DaysBeforeEvent !== null ? reminder.DaysBeforeEvent : reminder.WeeksBeforeEvent;
                                                let max = reminder.MinutesBeforeEvent !== null ? 40320 : reminder.HoursBeforeEvent !== null ? 672 : reminder.DaysBeforeEvent !== null ? 28 : 4;

                                                let selectedMinutes = reminder.MinutesBeforeEvent !== null ? 'selected' : '';
                                                let selectedHours = reminder.HoursBeforeEvent !== null ? 'selected' : '';
                                                let selectedDays = reminder.DaysBeforeEvent !== null ? 'selected' : '';
                                                let selectedWeeks = reminder.WeeksBeforeEvent !== null ? 'selected' : '';

                                                const selNotificationTimeTypeAllDayUnchecked = `
                                                <input type='number' class='form-control-sm viewCalendarEventSelNotificationNumberAllDayUnchecked' style='width:20%;text-overflow:ellipsis; background-color:#343a40; color:#fff; border-color:#6c757d;' min="0" max="${max}" value="${value}" disabled>
                                                <select class='form-control-sm viewCalendarEventSelNotificationTimeTypeAllDayUnchecked' style='width:15%;text-overflow:ellipsis;' disabled>
                                                    <option value='Minutes' ${selectedMinutes}>${localizer.Minutes}</option>
                                                    <option value='Hours' ${selectedHours}>${localizer.Hours}</option>
                                                    <option value='Days' ${selectedDays}>${localizer.Days}</option>
                                                    <option value='Weeks' ${selectedWeeks}>${localizer.Weeks}</option>
                                                </select>`;

                                                const htmlString = String.raw`
                                                    <div class="divViewEventNotificationAllDayUncheckedRow" style="padding-bottom:5px;">
                                                        <select class='form-control-sm viewCalendarEventSelNotificationMethodAllDayUnchecked' style='width:20%;text-overflow:ellipsis;' disabled>
                                                            ${selectedMethodOption}
                                                        </select>

                                                        ${selNotificationTimeTypeAllDayUnchecked}

                                                        <br />
                                                        <div class="error-message" style="color: red; font-size: 0.9em; display: none;"></div>
                                                    </div>
                                                `;

                                                $(htmlString).insertBefore($viewCalendarEventNotificationAllDayUnchecked);
                                            }
                                        });
                                    }
                                }

                                $viewCalendarEventTaskDialogModal.modal({
                                    keyboard: false,
                                    backdrop: "static"
                                });

                                $viewCalendarEventTaskDialogModal.modal('toggle');
                                $viewCalendarEventTaskDialogModal.modal('show');
                            }
                            else {
                                toastr.error(data.error);
                            }
                        }
                    });

                    popup.hide();
                });

                return false;
            }
        }
    });

    JSON.parse($otherCalendarEventOutputViewModels.val()).forEach(item => {
        calendar.addEvent({
            id: item.Id,
            title: item.Title,
            allDay: item.AllDay,
            start: item.StartDate,
            end: item.EndDate,
            backgroundColor: item.HtmlColorCode,
            borderColor: item.HtmlColorCode,
            extendedProps: {
                calendarId: item.CalendarId,
                displayStartDate: item.DisplayStartDate,
                displayEndDate: item.DisplayEndDate,
                displayStartDateTimeZone: item.DisplayStartDateTimeZone,
                displayEndDateTimeZone: item.DisplayEndDateTimeZone,
                calendarType: 'Other'
            }
        });
    });

    calendar.render();

    function RefreshCalendarEvents() {

        calendar.removeAllEvents();

        let paramValue = {
            Calendars: []
        };

        $otherCalendars.find('input[type="checkbox"]').filter(':checked').each(function () {
            let calendarsArray = [
                { Id: Number($(this).closest('label').attr('id').replace('lblOtherCalendar', '')) }
            ];

            for (let i = 0; i < calendarsArray.length; i++) {
                paramValue.Calendars.push(calendarsArray[i]);
            }
        });

        $.ajax({
            url: '/Calendar/GetCalendarEvents',
            method: 'POST',
            headers: { 'RequestVerificationToken': $__RequestVerificationToken.val() },
            dataType: 'json',
            data: JSON.stringify(paramValue),
            contentType: 'application/json; charset=utf-8',
            success: function (response) {
                if (response.result) {
                    JSON.parse(response.calendarEvents).forEach(item => {
                        calendar.addEvent({
                            id: item.Id,
                            title: item.Title,
                            allDay: item.AllDay,
                            start: item.StartDate,
                            end: item.EndDate,
                            backgroundColor: item.HtmlColorCode,
                            borderColor: item.HtmlColorCode,
                            extendedProps: {
                                calendarId: item.CalendarId,
                                displayStartDate: item.DisplayStartDate,
                                displayEndDate: item.DisplayEndDate,
                                displayStartDateTimeZone: item.DisplayStartDateTimeZone,
                                displayEndDateTimeZone: item.DisplayEndDateTimeZone,
                                calendarType: item.CalendarType
                            }
                        });
                    });
                }
            }
        });
    }

    $(document).off('change.AnonymousIndex', '.chkOtherCalendar').on('change.AnonymousIndex', '.chkOtherCalendar', function () {
        RefreshCalendarEvents();
    });
});

$aViewCalendarEventAttachedFile.off('click').on('click', function (event) {
    event.preventDefault();
    let base64Data = $(this).attr('data-file');
    let contentType = $(this).attr('data-contenttype');
    let name = $(this).attr('data-name')

    if (base64Data && contentType) {
        let blob = base64ToBlob(base64Data, contentType);
        let url = URL.createObjectURL(blob);
        let a = document.createElement('a');
        try {
            a.href = url;
            a.download = name;
            a.click();
        } finally {
            setTimeout(() => URL.revokeObjectURL(url), 100);
            a.remove();
        }
    }
});