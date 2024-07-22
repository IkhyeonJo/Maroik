// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$('#createEventDescription').summernote({
    //height: 300,
    callbacks: {
        onImageUpload: function (files) {
            for (let i = 0; i < files.length; i++) {
                WriteUploadImageFile(files[i]);
            }
        }
    }
});

$.datepicker.setDefaults({
    dateFormat: 'yy-mm-dd',
    prevText: 'Previous Month',
    nextText: 'Next Month',
    monthNames: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
    monthNamesShort: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
    dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
    dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
    dayNamesMin: ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'],
    showMonthAfterYear: true,
    yearSuffix: ''
});

$(function () {
    $("#createCalendarTabs").tabs();
    $("#editCalendarTabs").tabs();
    $("#createEventTaskTabs").tabs();
    //$("#editExpenditureTabs").tabs();
    $("#createEventAllDayUncheckedStartDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                // Custom logic before showing the datepicker for start date
            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {
                // Custom logic when the month/year changes in start date datepicker
            }, 1);
        },
        onSelect: function (selectedDate) {
            let startDate = $(this).datepicker('getDate');
            let endDate = $("#createEventAllDayUncheckedEndDate").datepicker('getDate');
            if (endDate && endDate < startDate) {
                $("#createEventAllDayUncheckedEndDate").datepicker('setDate', startDate);
            }
            $("#createEventAllDayUncheckedEndDate").datepicker('option', 'minDate', startDate);
        }
    });

    $("#createEventAllDayUncheckedEndDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                // Custom logic before showing the datepicker for end date
            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {
                // Custom logic when the month/year changes in end date datepicker
            }, 1);
        },
        onSelect: function (selectedDate) {
            let endDate = $(this).datepicker('getDate');
            let startDate = $("#createEventAllDayUncheckedStartDate").datepicker('getDate');
            if (startDate && endDate < startDate) {
                $(this).datepicker('setDate', startDate);
            }
        }
    });

    $("#createEventAllDayCheckedStartDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                // Custom logic before showing the datepicker for start date
            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {
                // Custom logic when the month/year changes in start date datepicker
            }, 1);
        },
        onSelect: function (selectedDate) {
            let startDate = $(this).datepicker('getDate');
            let endDate = $("#createEventAllDayCheckedEndDate").datepicker('getDate');
            if (endDate && endDate < startDate) {
                $("#createEventAllDayCheckedEndDate").datepicker('setDate', startDate);
            }
            $("#createEventAllDayCheckedEndDate").datepicker('option', 'minDate', startDate);
        }
    });

    $("#createEventAllDayCheckedEndDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                // Custom logic before showing the datepicker for end date
            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {
                // Custom logic when the month/year changes in end date datepicker
            }, 1);
        },
        onSelect: function (selectedDate) {
            let endDate = $(this).datepicker('getDate');
            let startDate = $("#createEventAllDayCheckedStartDate").datepicker('getDate');
            if (startDate && endDate < startDate) {
                $(this).datepicker('setDate', startDate);
            }
        }
    });

    //$("#editExpenditureDate").datepicker({
    //    showButtonPanel: true,
    //    beforeShow: function (input) {
    //        setTimeout(function () {

    //        }, 1);
    //    },
    //    onChangeMonthYear: function (year, month, instance) {
    //        setTimeout(function () {

    //        }, 1);
    //    }
    //});

    $('<style type="text/css"> .ui-datepicker-close { display: none; } </style>').appendTo("head");
    $('<style type="text/css"> .ui-datepicker-current { display: none; } </style>').appendTo("head");

    if ($("#createEventAllDay").is(':checked')) {
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

    function ini_events(element) {
        element.each(function () {

            let eventObject = {
                title: $.trim($(this).text())
            }

            $(this).data('eventObject', eventObject)

            $(this).draggable({
                zIndex: 1070,
                revert: true,
                revertDuration: 0
            })

        })
    }

    ini_events($('#external-events div.external-event'))

    let date = new Date()
    let d = date.getDate(),
        m = date.getMonth(),
        y = date.getFullYear()

    let Calendar = FullCalendar.Calendar;
    let Draggable = FullCalendar.Draggable;

    let containerEl = document.getElementById('external-events');
    let checkbox = document.getElementById('drop-remove');
    let calendarEl = document.getElementById('calendar');

    new Draggable(containerEl, {
        itemSelector: '.external-event',
        eventData: function (eventEl) {
            return {
                title: eventEl.innerText,
                backgroundColor: window.getComputedStyle(eventEl, null).getPropertyValue('background-color'),
                borderColor: window.getComputedStyle(eventEl, null).getPropertyValue('background-color'),
                textColor: window.getComputedStyle(eventEl, null).getPropertyValue('color'),
            };
        }
    });

    let calendar = new Calendar(calendarEl, {
        headerToolbar: {
            left: 'prevYear,prev,next,nextYear today',
            center: 'title',
            right: 'dayGridMonth,dayGridWeek,dayGridDay'
        },
        themeSystem: 'bootstrap',
        events: [
            {
                title: 'All Day Event',
                start: new Date(y, m, 1),
                backgroundColor: '#f56954', //red
                borderColor: '#f56954', //red
                allDay: true
            },
            {
                title: 'Long Event',
                start: new Date(y, m, d - 5),
                end: new Date(y, m, d - 2),
                backgroundColor: '#f39c12', //yellow
                borderColor: '#f39c12' //yellow
            },
            {
                title: 'Meeting',
                start: new Date(y, m, d, 10, 30),
                allDay: false,
                backgroundColor: '#0073b7', //Blue
                borderColor: '#0073b7' //Blue
            },
            {
                title: 'Lunch',
                start: new Date(y, m, d, 12, 0),
                end: new Date(y, m, d, 14, 0),
                allDay: false,
                backgroundColor: '#00c0ef', //Info (aqua)
                borderColor: '#00c0ef' //Info (aqua)
            },
            {
                title: 'Birthday Party',
                start: new Date(y, m, d + 1, 19, 0),
                end: new Date(y, m, d + 1, 22, 30),
                allDay: false,
                backgroundColor: '#00a65a', //Success (green)
                borderColor: '#00a65a' //Success (green)
            },
            {
                title: 'Click for Google',
                start: new Date(y, m, 28),
                end: new Date(y, m, 29),
                url: 'https://www.google.com/',
                backgroundColor: '#3c8dbc', //Primary (light-blue)
                borderColor: '#3c8dbc' //Primary (light-blue)
            }
        ],
        //events:
        //    [
        //        {
        //            id: 1,
        //            title: 'First Event',
        //            start: '2020-08-08T10:30:00',
        //            end: '2020-08-08T11:30:00',
        //            extendedProps: {
        //                description: 'Test 1'
        //            },
        //            color: '#336e03'
        //        },

        //        {
        //            id: 2,
        //            title: 'Second Event',
        //            start: '2020-08-08T15:30:00',
        //            end: '2020-08-08T16:30:00',
        //            extendedProps: {
        //                description: 'Test 2'
        //            },
        //        }
        //    ],
        timezone: 'UTC',
        navLinks: true, // can click day/week names to navigate views
        editable: true,
        droppable: true, // this allows things to be dropped onto the calendar !!!
        drop: function (info) {
            // is the "remove after drop" checkbox checked?
            if (checkbox.checked) {
                // if so, remove the element from the "Draggable Events" list
                info.draggedEl.parentNode.removeChild(info.draggedEl);
            }
        },
        eventDrop: function (info) {
            alert(info.event.title + " was dropped on id: " + info.event.id);
            alert(info.event.title + " was dropped on " + info.oldEvent.start.toISOString());
            alert(info.event.title + " was dropped on " + info.event.start.toISOString());

            //if (!confirm("Are you sure about this change?")) {
            //    info.revert();
            //}
        },
        selectable: true,
        selectMirror: true,
        select: function (arg) {
            //let aaa = $('#calendarEventModel').val();
            //let bbb = JSON.parse($('#calendarEventModel').val());

            $('#createEventTaskDialogModal').modal('show');

            $('#createEventAllDayUncheckedStartDate').val(moment(arg.start).format("YYYY-MM-DD"));
            $('#createEventAllDayCheckedStartDate').val(moment(arg.start).format("YYYY-MM-DD"));

            $('#createEventAllDayUncheckedEndDate').val(moment(arg.end).format("YYYY-MM-DD"));
            $('#createEventAllDayCheckedEndDate').val(moment(arg.end).format("YYYY-MM-DD"));

            calendar.unselect();
        },
        eventClick: function (arg) {

            alert("clicked event id: " + arg.event.id);

            if (confirm('Are you sure you want to delete this event?')) {
                arg.event.remove()
            }
        }
    });

    calendar.addEvent({
        id: 551,
        title: 'Maroik',
        start: '2024-06-25T15:30:00',
        end: '2024-06-25T16:30:00',
        allDay: false
    });

    calendar.render();
    // $('#calendar').fullCalendar()

    /* ADDING EVENTS */
    let currColor = '#3c8dbc' //Red by default
    // Color chooser button
    $('#color-chooser > li > a').off('click').on('click', function (e) {
        e.preventDefault()
        // Save color
        currColor = $(this).css('color')
        // Add color effect to button
        $('#add-new-event').css({
            'background-color': currColor,
            'border-color': currColor
        })
    })
    $('#add-new-event').off('click').on('click', function (e) {
        e.preventDefault()
        // Get value and make sure it is not null
        let val = $('#new-event').val()
        if (val.length == 0) {
            return
        }

        // Create events
        let event = $('<div />')
        event.css({
            'background-color': currColor,
            'border-color': currColor,
            'color': '#fff'
        }).addClass('external-event')
        event.text(val)
        $('#external-events').prepend(event)

        // Add draggable funtionality
        ini_events(event)

        // Remove event from text input
        $('#new-event').val('')
    })

    function ValidateInputAllDayChecked(row) {
        let numberInput = row.find('.createEventSelNotificationNumberAllDayChecked');
        let value = numberInput.val().trim();
        let timeType = row.find('.createEventSelNotificationTimeTypeAllDayChecked').val();
        let isValid = true;
        let errorMessage = '';

        if (value === '') {
            isValid = false;
            errorMessage = 'This field is required';
        } else if (isNaN(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid number';
        } else {
            value = parseInt(value, 10);
            if (timeType === 'Days') {
                if (value < 0 || value > 28) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 28 days';
                }
            }
            else if (timeType === 'Weeks') {
                if (value < 0 || value > 4) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 4 weeks';
                }
            }
        }

        if (!isValid) {
            numberInput.css('text-decoration', 'underline');
            numberInput.css('text-decoration-color', 'red');
        } else {
            numberInput.css('text-decoration', 'none');
        }

        row.find('.error-message').text(errorMessage).toggle(!isValid);
        return isValid;
    }

    function ValidateInputAllDayUnchecked(row) {
        let numberInput = row.find('.createEventSelNotificationNumberAllDayUnchecked');
        let value = numberInput.val().trim();
        let timeType = row.find('.createEventSelNotificationTimeTypeAllDayUnchecked').val();
        let isValid = true;
        let errorMessage = '';

        if (value === '') {
            isValid = false;
            errorMessage = 'This field is required';
        } else if (isNaN(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid number';
        } else {
            value = parseInt(value, 10);
            if (timeType === 'Minutes') {
                if (value < 0 || value > 40320) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 40,320 minutes';
                }
            }
            else if (timeType === 'Hours') {
                if (value < 0 || value > 672) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 672 hours';
                }
            }
            else if (timeType === 'Days') {
                if (value < 0 || value > 28) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 28 days';
                }
            }
            else if (timeType === 'Weeks') {
                if (value < 0 || value > 4) {
                    isValid = false;
                    errorMessage = 'Must be between 0 and 4 weeks';
                }
            }
        }

        if (!isValid) {
            numberInput.css('text-decoration', 'underline');
            numberInput.css('text-decoration-color', 'red');
        } else {
            numberInput.css('text-decoration', 'none');
        }

        row.find('.error-message').text(errorMessage).toggle(!isValid);
        return isValid;
    }

    $(document).on('change', '.createEventSelNotificationTimeTypeAllDayChecked', function () {
        let row = $(this).closest('.divCreateEventNotificationAllDayCheckedRow');
        ValidateInputAllDayChecked(row);
        row.find('.error-message').hide();
    });

    $(document).on('input', '.createEventSelNotificationNumberAllDayChecked', function () {
        let row = $(this).closest('.divCreateEventNotificationAllDayCheckedRow');
        ValidateInputAllDayChecked(row);
        row.find('.error-message').hide();
    });

    $(document).on('change', '.createEventSelNotificationTimeTypeAllDayUnchecked', function () {
        let row = $(this).closest('.divCreateEventNotificationAllDayUncheckedRow');
        ValidateInputAllDayUnchecked(row);
        row.find('.error-message').hide();
    });

    $(document).on('input', '.createEventSelNotificationNumberAllDayUnchecked', function () {
        let row = $(this).closest('.divCreateEventNotificationAllDayUncheckedRow');
        ValidateInputAllDayUnchecked(row);
        row.find('.error-message').hide();
    });

    $('#formCreateEvent').off('submit').on('submit', function (event) {
        event.preventDefault();

        let isValidForm = true;

        if ($('#createEventAllDay').is(':checked')) {
            $('.divCreateEventNotificationAllDayCheckedRow').each(function () {
                let isValidRow = ValidateInputAllDayChecked($(this));
                isValidForm = isValidForm && isValidRow;
            });
        }
        else {
            $('.divCreateEventNotificationAllDayUncheckedRow').each(function () {
                let isValidRow = ValidateInputAllDayUnchecked($(this));
                isValidForm = isValidForm && isValidRow;
            });
        }

        if (isValidForm) {
            alert('Form submitted successfully');
        }

    });
});