import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, hidden } from "@tests/_common/harness";

// wwwroot/admin/custom/Calendar/AdminIndex/js/site.js  (2,279 lines)

const LOCALIZERS = [
  "Email", "Notification", "Minutes", "Hours", "Days", "Weeks", "BeforeAt", "IETFLanguageTag",
  "Today", "Month", "ConfirmDelete", "ThisFieldRequired", "ErrorInvalidNumber",
  "ErrorRangeMinute", "ErrorRangeHour", "ErrorRangeDay", "ErrorRangeWeek",
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
     <form id="formCreateCalendar"></form><form id="formEditCalendar"></form>
     <form id="formCreateCalendarEvent"></form><form id="formEditCalendarEvent"></form>
     <div id="myCalendars"></div>
     <a id="aEditCalendarEventAttachedFile"></a>
     <div class="divCreateEventNotificationAllDayCheckedRow">
       <input class="createCalendarEventSelNotificationNumberAllDayChecked" />
       <select class="createCalendarEventSelNotificationTimeTypeAllDayChecked">
         <option value="Days" selected>Days</option><option value="Weeks">Weeks</option>
       </select>
       <div class="error-message" style="display:none"></div>
     </div>`
  );
}

describe("admin/Calendar/AdminIndex", () => {
  it("loads without throwing (FullCalendar / moment / datepicker stubs suffice)", () => {
    expect(() => loadSite("admin", "Calendar", "AdminIndex", fixture())).not.toThrow();
  });

  it("flags a day-reminder above CalendarReminderPolicy.MaxDaysBeforeEvent (28)", () => {
    const h = loadSite("admin", "Calendar", "AdminIndex", fixture());
    const row = h.$(".divCreateEventNotificationAllDayCheckedRow");
    row.find(".createCalendarEventSelNotificationNumberAllDayChecked").val("99").trigger("input");
    expect(row.find(".error-message").text()).toBe("L_ErrorRangeDay");
  });

  it("accepts an in-range day reminder", () => {
    const h = loadSite("admin", "Calendar", "AdminIndex", fixture());
    const row = h.$(".divCreateEventNotificationAllDayCheckedRow");
    row.find(".createCalendarEventSelNotificationNumberAllDayChecked").val("7").trigger("input");
    expect(row.find(".error-message").text()).toBe("");
  });

  it("'add event' fetches the calendar list from /Calendar/GetCalendars", () => {
    const h = loadSite("admin", "Calendar", "AdminIndex", fixture());
    h.$("#aCreateCalendarEvent").trigger("click");
    expect(h.ajaxCalls.some((c) => c.url === "/Calendar/GetCalendars")).toBe(true);
  });
});
