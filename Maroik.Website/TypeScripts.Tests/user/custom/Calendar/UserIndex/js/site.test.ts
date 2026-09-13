import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, hidden } from "@tests/_common/harness";

// wwwroot/user/custom/Calendar/UserIndex/js/site.js  (2,775 lines)

const LOCALIZERS = [
  "Email", "Notification", "Minutes", "Hours", "Days", "Weeks", "BeforeAt", "IETFLanguageTag",
  "Today", "Month", "ConfirmDelete", "ThisFieldRequired", "ErrorInvalidNumber",
  "ErrorRangeMinute", "ErrorRangeHour", "ErrorRangeDay", "ErrorRangeWeek", "FailedToLoadCalendars",
];

function fixture(): string {
  return (
    antiForgery +
    hidden("maxAttachedFileSizeBytes", "1048576") +
    hidden("maxMinutesBeforeEvent", "40320") +
    hidden("maxHoursBeforeEvent", "672") +
    hidden("maxDaysBeforeEvent", "28") +
    hidden("maxWeeksBeforeEvent", "4") +
    hidden("reminderTimeIntervals", "[&quot;09:00&quot;]") +
    hidden("defaultReminderTimeOfDay", "09:00") +
    hidden("loggedInAccountTimeZoneIanaId", "Asia/Seoul") +
    hidden("calendarEventOutputViewModels", "[]") +
    hidden("otherCalendarEventOutputViewModels", "[]") +
    LOCALIZERS.map((n) => hidden(`localizer${n}`, `L_${n}`)).join("") +
    `<div id="calendar"></div>
     <div id="createCalendarTabs"></div><div id="editCalendarTabs"></div>
     <div id="createCalendarEventTaskTabs"></div><div id="editCalendarEventTaskTabs"></div>
     <div id="viewCalendarEventTaskTabs"></div><div id="browseCalendarsOfInterestTabs"></div>
     <div id="createCalendarEventDescription"></div><div id="editCalendarEventDescription"></div>
     <div id="viewCalendarEventDescription"></div>
     <input type="file" id="createCalendarEventAttachment" data-errorMessage="big" />
     <input type="file" id="editCalendarEventAttachment" data-errorMessage="big" />
     <input type="checkbox" id="createCalendarEventAllDay" /><input type="checkbox" id="editCalendarEventAllDay" />
     ${["createCalendarEventAllDayUncheckedStartDate", "createCalendarEventAllDayUncheckedEndDate",
       "createCalendarEventAllDayCheckedStartDate", "createCalendarEventAllDayCheckedEndDate",
       "editCalendarEventAllDayUncheckedStartDate", "editCalendarEventAllDayUncheckedEndDate",
       "editCalendarEventAllDayCheckedStartDate", "editCalendarEventAllDayCheckedEndDate"]
       .map((id) => `<input id="${id}" />`).join("")}
     ${["divCreateEventAllDayUnchecked", "divCreateEventAllDayChecked",
        "divCreateEventNotificationAllDayUnchecked", "divCreateEventNotificationAllDayChecked",
        "divEditEventAllDayUnchecked", "divEditEventAllDayChecked",
        "divEditEventNotificationAllDayUnchecked", "divEditEventNotificationAllDayChecked"]
       .map((id) => `<div id="${id}"></div>`).join("")}
     <select id="createCalendarEventMyCalendar"></select><select id="editCalendarEventMyCalendar"></select>
     <div id="dropdown-content"></div><i id="dropdown-icon"></i>
     <a id="aCreateCalendar"></a><a id="aCreateCalendarEvent"></a>
     <form id="formCreateCalendar">
       <input id="createCalendarName" /><input id="createCalendarDescription" />
       <input id="createCalendarHtmlColorCode" value="#3788d8" /><input id="createCalendarTimeZone" value="UTC" />
     </form>
     <form id="formEditCalendar"></form>
     <form id="formCreateCalendarEvent"></form><form id="formEditCalendarEvent"></form>
     <div id="myCalendars"></div>
     <a id="aEditCalendarEventAttachedFile"></a><a id="aViewCalendarEventAttachedFile"></a>
     <a id="editCalendarEventPopup" href="#"></a><input id="editCalendarEventStatus" />
     <a id="viewOtherCalendarEventPopup" href="#"></a><input id="viewCalendarEventStatus" />
     <select id="viewCalendarEventMyCalendar"></select>
     <a id="aBrowseCalendarsOfInterest"></a><input type="checkbox" id="allCheckBrowseCalendarsOfInterest" />
     <div id="otherCalendars"></div><div id="divBrowseCalendarsOfInterest"></div>
     <form id="formUpdateBrowseCalendarsOfInterest"></form>
     <div id="browseCalendarsOfInterestDialogModal"></div>
     <div class="divCreateEventNotificationAllDayCheckedRow">
       <input class="createCalendarEventSelNotificationNumberAllDayChecked" />
       <select class="createCalendarEventSelNotificationTimeTypeAllDayChecked">
         <option value="Days" selected>Days</option><option value="Weeks">Weeks</option>
       </select>
       <div class="error-message" style="display:none"></div>
     </div>`
  );
}

describe("user/Calendar/UserIndex", () => {
  it("loads without throwing", () => {
    expect(() => loadSite("user", "Calendar", "UserIndex", fixture())).not.toThrow();
  });

  it("flags a day-reminder above CalendarReminderPolicy.MaxDaysBeforeEvent (28)", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    const row = h.$(".divCreateEventNotificationAllDayCheckedRow");
    row.find(".createCalendarEventSelNotificationNumberAllDayChecked").val("40").trigger("input");
    expect(row.find(".error-message").text()).toBe("L_ErrorRangeDay");
  });

  it("'add event' fetches /Calendar/GetCalendars", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    h.$("#aCreateCalendarEvent").trigger("click");
    expect(h.ajaxCalls.some((c) => c.url === "/Calendar/GetCalendars")).toBe(true);
  });

  // Regression: this call used to set `async: false`, freezing the page for the round trip.
  // It must run asynchronous, with the modal opened from `complete` once the fetch settles.
  it("'add event' fetches the calendar list asynchronously and opens the modal once it settles", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    h.$("#aCreateCalendarEvent").trigger("click");

    const call = h.lastAjax();
    expect(call.async).not.toBe(false);

    call.success?.({ result: true, calendars: [{ id: 1, name: "Cal 1" }] });
    call.complete?.();

    expect(h.$("#createCalendarEventMyCalendar option").length).toBe(1);
  });

  // Regression: a fetch failure used to have no `error` handler at all; the create-event
  // modal must still open (degraded — empty calendar select) instead of silently hanging.
  it("'add event' shows an error toast and still opens the modal when the fetch fails", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    h.$("#aCreateCalendarEvent").trigger("click");

    const call = h.lastAjax();
    call.error?.();
    call.complete?.();

    expect(h.toastr.error).toHaveBeenCalledWith("L_FailedToLoadCalendars");
  });

  // Regression: the "browse calendars of interest" dialog used `async: false` to guarantee
  // the row list was built before the modal opened; it must now defer to `complete` instead.
  it("'browse calendars of interest' dialog fetches asynchronously and opens once settled", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    h.$("#aBrowseCalendarsOfInterest").trigger("click");

    const call = h.lastAjax();
    expect(call.url).toBe("/Calendar/GetBrowseCalendarsOfInterest");
    expect(call.async).not.toBe(false);

    call.success?.({ result: true, browseCalendarsOfInterests: [{ id: 1, name: "Cal 1", checked: false }] });
    call.complete?.();

    expect(h.win.document.getElementById("divBrowseCalendarsOfInterest")!.innerHTML).toContain("Cal 1");
  });

  /** Minimal payload accepted by IsCalendarEventExists / IsOtherCalendarEventExists's success handlers. */
  function makeCalendarEventPayload(overrides: Partial<Record<string, unknown>> = {}) {
    return {
      result: true,
      calendarEvent: {
        id: 1,
        title: "Team sync",
        allDay: true,
        displayStartDate: "2024-01-01",
        displayEndDate: "2024-01-02",
        location: "",
        description: "<p>hello</p>",
        calendarEventAttachedFile: null,
        calendarId: 5,
        status: "Confirmed",
        serializedCalendarReminders: "[]",
        ...overrides,
      },
    };
  }

  // Regression: the drag-select "new event" flow used `async: false` on its GetCalendars fetch.
  // It must run asynchronous, with the modal opened from `complete` once the fetch settles.
  it("drag-selecting a date range fetches the calendar list asynchronously and opens the modal once settled", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());

    h.calendarOptions[0].select({ start: new Date("2024-01-01"), end: new Date("2024-01-02") });

    const call = h.lastAjax();
    expect(call.url).toBe("/Calendar/GetCalendars");
    expect(call.async).not.toBe(false);

    call.success?.({ result: true, calendars: [{ id: 1, name: "Cal 1" }] });
    call.complete?.();

    expect(h.$("#createCalendarEventMyCalendar option").length).toBe(1);
  });

  /** Drives the "click a 'My' event -> click 'edit' in its popup" flow via the captured `eventClick` callback. */
  function openEditPopup(h: ReturnType<typeof loadSite>): void {
    const eventEl = h.win.document.createElement("div");
    h.win.document.body.appendChild(eventEl);
    h.calendarOptions[0].eventClick({
      el: eventEl,
      event: {
        id: 1,
        title: "Team sync",
        allDay: true,
        extendedProps: { calendarType: "My", displayStartDate: "2024-01-01", displayEndDate: "2024-01-02" },
      },
    });
    h.$("#editCalendarEventPopup").trigger("click");
  }

  // Regression: the edit-event modal's GetCalendars fetch used `async: false` too; it must run
  // asynchronous, with the rest of the edit modal built from `complete` once settled.
  it("editing a 'My' event fetches the calendar list asynchronously and still builds the modal once settled", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    openEditPopup(h);

    const outerCall = h.lastAjax();
    expect(outerCall.url).toContain("/Calendar/IsCalendarEventExists");
    outerCall.success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    expect(innerCall.url).toBe("/Calendar/GetCalendars");
    expect(innerCall.async).not.toBe(false);

    innerCall.success?.({ result: true, calendars: [{ id: 5, name: "Cal 5" }] });
    innerCall.complete?.();

    expect(h.$("#editCalendarEventStatus").val()).toBe("Confirmed");
    expect(h.$("#editCalendarEventMyCalendar option").length).toBe(1);
  });

  // Regression: a fetch failure used to have no `error` handler; the edit modal must still be
  // built (degraded — empty calendar select) instead of silently never opening.
  it("editing a 'My' event shows an error toast and still builds the modal when GetCalendars fails", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    openEditPopup(h);

    h.lastAjax().success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    innerCall.error?.();
    innerCall.complete?.();

    expect(h.toastr.error).toHaveBeenCalledWith("L_FailedToLoadCalendars");
    expect(h.$("#editCalendarEventStatus").val()).toBe("Confirmed");
  });

  /** Drives the "click an 'Other' event -> click 'view' in its popup" flow via `eventClick`. */
  function openViewPopup(h: ReturnType<typeof loadSite>): void {
    const eventEl = h.win.document.createElement("div");
    h.win.document.body.appendChild(eventEl);
    h.calendarOptions[0].eventClick({
      el: eventEl,
      event: {
        id: "other-1",
        title: "Team sync",
        allDay: true,
        extendedProps: { calendarType: "Other", displayStartDate: "2024-01-01", displayEndDate: "2024-01-02" },
      },
    });
    h.$("#viewOtherCalendarEventPopup").trigger("click");
  }

  // Regression: the read-only view modal's GetOtherCalendars fetch used `async: false` too; it
  // must run asynchronous, with the rest of the modal built from `complete` once settled.
  it("viewing an 'Other' event fetches the calendar list asynchronously and still builds the modal once settled", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    openViewPopup(h);

    const outerCall = h.lastAjax();
    expect(outerCall.url).toContain("/Calendar/IsOtherCalendarEventExists");
    outerCall.success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    expect(innerCall.url).toBe("/Calendar/GetOtherCalendars");
    expect(innerCall.async).not.toBe(false);

    innerCall.success?.({ result: true, tempOtherCalendars: [{ id: 5, name: "Shared" }] });
    innerCall.complete?.();

    expect(h.$("#viewCalendarEventStatus").val()).toBe("Confirmed");
    expect(h.$("#viewCalendarEventMyCalendar option").length).toBe(1);
  });

  // Regression: a fetch failure used to have no `error` handler; the read-only modal must still
  // be built (degraded — empty calendar select) instead of silently never opening.
  it("viewing an 'Other' event shows an error toast and still builds the modal when GetOtherCalendars fails", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    openViewPopup(h);

    h.lastAjax().success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    innerCall.error?.();
    innerCall.complete?.();

    expect(h.toastr.error).toHaveBeenCalledWith("L_FailedToLoadCalendars");
    expect(h.$("#viewCalendarEventStatus").val()).toBe("Confirmed");
  });

  // Regression: a calendar name is free text the owner controls. It used to be interpolated
  // straight into a raw HTML template literal with no escaping, so a name containing markup would
  // be parsed as HTML by every viewer who saw it (the sidebar list here, and the "browse calendars
  // of interest" dialog below) instead of being displayed as plain text.
  it("escapes a malicious calendar name before appending it to the sidebar list", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());
    h.$("#createCalendarName").val("<img src=x onerror=alert(1)>");

    h.$("#formCreateCalendar").trigger("submit");
    const call = h.lastAjax();
    expect(call.url).toBe("/Calendar/CreateCalendar");
    call.success?.({ result: true, calendar: { id: 1, name: "<img src=x onerror=alert(1)>", htmlColorCode: "#3788d8" } });

    const myCalendarsHtml = h.win.document.getElementById("myCalendars")!.innerHTML;
    expect(myCalendarsHtml).not.toContain("<img");
    expect(myCalendarsHtml).toContain("&lt;img src=x onerror=alert(1)&gt;");
  });

  it("escapes a malicious calendar name in the 'browse calendars of interest' dialog", () => {
    const h = loadSite("user", "Calendar", "UserIndex", fixture());

    h.$("#aBrowseCalendarsOfInterest").trigger("click");
    const call = h.lastAjax();
    expect(call.url).toBe("/Calendar/GetBrowseCalendarsOfInterest");
    call.success?.({
      result: true,
      browseCalendarsOfInterests: [{ id: 1, name: "<img src=x onerror=alert(1)>", checked: false }],
    });

    const html = h.win.document.getElementById("divBrowseCalendarsOfInterest")!.innerHTML;
    expect(html).not.toContain("<img");
    expect(html).toContain("&lt;img src=x onerror=alert(1)&gt;");
  });
});
