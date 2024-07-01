// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$.datepicker.setDefaults({
    dateFormat: 'yy-mm-dd',
    prevText: '이전 달',
    nextText: '다음 달',
    monthNames: ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'],
    monthNamesShort: ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'],
    dayNames: ['일', '월', '화', '수', '목', '금', '토'],
    dayNamesShort: ['일', '월', '화', '수', '목', '금', '토'],
    dayNamesMin: ['일', '월', '화', '수', '목', '금', '토'],
    showMonthAfterYear: true,
    yearSuffix: '년'
});

const currentYear = $("#createEventStartDate").val().split('-')[0];
const currentMonth = $("#createEventStartDate").val().split('-')[1];
const currentDay = $("#createEventStartDate").val().split('-')[2];

$(function () {
    $("#createEventTaskTabs").tabs();
    //$("#editExpenditureTabs").tabs();
    $("#createEventStartDate").datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {

            }, 1);
        },
        onChangeMonthYear: function (year, month, instance) {
            setTimeout(function () {

            }, 1);
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

    function ini_events(element) {
        element.each(function () {

            var eventObject = {
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

    var date = new Date()
    var d = date.getDate(),
        m = date.getMonth(),
        y = date.getFullYear()

    var Calendar = FullCalendar.Calendar;
    var Draggable = FullCalendar.Draggable;

    var containerEl = document.getElementById('external-events');
    var checkbox = document.getElementById('drop-remove');
    var calendarEl = document.getElementById('calendar');

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

    var calendar = new Calendar(calendarEl, {
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
            var aaa = $('#calendarEventModel').val();
            var bbb = JSON.parse($('#calendarEventModel').val());

            let modal = $('#createEventTaskDialogModal');
            modal.modal('show');

            alert(arg.start);

            //var title = prompt('Event Title:');
            //if (title) {
            //    calendar.addEvent({
            //        title: title,
            //        start: arg.start,
            //        end: arg.end,
            //        allDay: arg.allDay
            //    })
            //}

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
    var currColor = '#3c8dbc' //Red by default
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
        var val = $('#new-event').val()
        if (val.length == 0) {
            return
        }

        // Create events
        var event = $('<div />')
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
});