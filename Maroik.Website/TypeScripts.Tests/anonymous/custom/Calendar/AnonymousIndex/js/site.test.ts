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
    ["localizerIETFLanguageTag", "localizerToday", "localizerMonth", "localizerEmail", "localizerNotification"]
      .map((id) => hidden(id, "x")).join("") +
    `<div id="calendar"></div><div id="viewCalendarEventTaskTabs"></div>
     <div id="otherCalendarEventPopup"></div><div id="viewCalendarEventDescription"></div>
     <div id="otherCalendars"></div>
     <select id="viewCalendarEventMyCalendar"></select>
     <a id="aViewCalendarEventAttachedFile"></a>`
  );
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
});
