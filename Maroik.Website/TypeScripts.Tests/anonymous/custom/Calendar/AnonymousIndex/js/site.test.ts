import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, hidden } from "@tests/_common/harness";

// wwwroot/anonymous/custom/Calendar/AnonymousIndex/js/site.js  (read-only calendar view)
function fixture(): string {
  return (
    antiForgery +
    hidden("maxMinutesBeforeEvent", "40320") + hidden("maxHoursBeforeEvent", "672") +
    hidden("maxDaysBeforeEvent", "28") + hidden("maxWeeksBeforeEvent", "4") +
    hidden("reminderTimeIntervals", "[&quot;09:00&quot;]") + hidden("defaultReminderTimeOfDay", "09:00") +
    hidden("otherCalendarEventOutputViewModels", "[]") +
    ["localizerIETFLanguageTag", "localizerToday", "localizerMonth", "localizerEmail", "localizerNotification",
      "localizerFailedToLoadCalendars"]
      .map((id) => hidden(id, "x")).join("") +
    `<div id="calendar"></div><div id="viewCalendarEventTaskTabs"></div>
     <div id="otherCalendarEventPopup"></div>
     <div id="otherCalendarEventPopupTitle"></div>
     <div id="divOtherCalendarEventPopupAllDayChecked"></div><div id="divOtherCalendarEventPopupAllDayUnchecked"></div>
     <span id="otherCalendarEventPopupStartAllDayChecked"></span><span id="otherCalendarEventPopupEndAllDayChecked"></span>
     <span id="otherCalendarEventPopupStartAllDayUnchecked"></span><span id="otherCalendarEventPopupStartTimeZoneAllDayUnchecked"></span>
     <span id="otherCalendarEventPopupEndAllDayUnchecked"></span><span id="otherCalendarEventPopupEndTimeZoneAllDayUnchecked"></span>
     <a id="viewOtherCalendarEventPopup" href="#"></a><a id="closeOtherCalendarEventPopup" href="#"></a>
     <input id="viewCalendarEventId" /><input id="viewCalendarEventName" /><input type="checkbox" id="viewCalendarEventAllDay" />
     <div id="divViewEventAllDayChecked"></div><div id="divViewEventAllDayUnchecked"></div>
     <input id="viewCalendarEventAllDayCheckedStartDate" /><input id="viewCalendarEventAllDayCheckedEndDate" />
     <input id="viewCalendarEventAllDayUncheckedStartDate" /><input id="viewCalendarEventAllDayUncheckedStartTime" /><input id="viewCalendarEventAllDayUncheckedStartTimeZone" />
     <input id="viewCalendarEventAllDayUncheckedEndDate" /><input id="viewCalendarEventAllDayUncheckedEndTime" /><input id="viewCalendarEventAllDayUncheckedEndTimeZone" />
     <input id="viewCalendarEventLocation" />
     <div id="viewCalendarEventDescription"></div>
     <div id="divViewCalendarEventAttachedFile"></div>
     <span id="spanViewCalendarEventAttachedFile"></span>
     <select id="viewCalendarEventMyCalendar"></select>
     <input id="viewCalendarEventStatus" />
     <div id="divViewEventNotificationAllDayChecked"></div><div id="divViewEventNotificationAllDayUnchecked"></div>
     <div id="viewCalendarEventNotificationAllDayChecked"></div><div id="viewCalendarEventNotificationAllDayUnchecked"></div>
     <div id="viewCalendarEventTaskDialogModal"></div>
     <div id="otherCalendars"></div>
     <a id="aViewCalendarEventAttachedFile"></a>`
  );
}

/** Minimal payload accepted by IsOtherCalendarEventExists's success handler. */
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

describe("Calendar/AnonymousIndex", () => {
  it("loads and renders the (stubbed) FullCalendar without throwing", () => {
    expect(() => loadSite("anonymous", "Calendar", "AnonymousIndex", fixture())).not.toThrow();
  });

  it("re-fetches events when an other-calendar checkbox changes", () => {
    const h = loadSite("anonymous", "Calendar", "AnonymousIndex", fixture());
    h.win.document.body.insertAdjacentHTML("beforeend", '<input type="checkbox" class="chkOtherCalendar" />');
    h.$(".chkOtherCalendar").trigger("change");
    expect(h.ajaxCalls.some((c) => c.url === "/Calendar/GetCalendarEvents")).toBe(true);
  });

  /**
   * Drives the "click an Other event -> click 'view' in its popup" flow via the captured
   * FullCalendar `eventClick` callback (never actually invoked by the stubbed FullCalendar
   * itself), since that's the only way to reach the GetOtherCalendars fetch this fix touched.
   */
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

  // Regression: GetOtherCalendars used to set `async: false`, freezing the page for the round
  // trip. It must run asynchronous, with the read-only modal built from `complete` once settled.
  it("'view' fetches the calendar list asynchronously and still builds the modal once it settles", () => {
    const h = loadSite("anonymous", "Calendar", "AnonymousIndex", fixture());
    openViewPopup(h);

    const outerCall = h.lastAjax();
    expect(outerCall.url).toContain("/Calendar/IsOtherCalendarEventExists");
    outerCall.success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    expect(innerCall.url).toBe("/Calendar/GetOtherCalendars");
    expect(innerCall.async).not.toBe(false);

    innerCall.success?.({ result: true, tempOtherCalendars: [{ id: 5, name: "Shared" }] });
    innerCall.complete?.();

    // Only set inside showViewCalendarEventModal, which now runs from `complete`.
    expect(h.$("#viewCalendarEventStatus").val()).toBe("Confirmed");
    expect(h.$("#viewCalendarEventMyCalendar option").length).toBe(1);
  });

  // Regression: a fetch failure used to have no `error` handler; the read-only modal must still
  // be built (degraded — empty calendar select) instead of silently never opening.
  it("'view' shows an error toast and still builds the modal when GetOtherCalendars fails", () => {
    const h = loadSite("anonymous", "Calendar", "AnonymousIndex", fixture());
    openViewPopup(h);

    h.lastAjax().success?.(makeCalendarEventPayload());

    const innerCall = h.lastAjax();
    innerCall.error?.();
    innerCall.complete?.();

    expect(h.toastr.error).toHaveBeenCalledWith("x");
    expect(h.$("#viewCalendarEventStatus").val()).toBe("Confirmed");
  });
});
