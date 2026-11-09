---
paths:
  - "**/*Calendar*"
  - "**/Calendars/**"
  - "**/Calendar/**"
  - "**/*Reminder*"
  - "Maroik.Worker/**"
  - "Maroik.Worker.Tests/**"
---

# Intended design: Calendar

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- `GetCalendarEvents` loads every event of the selected calendars at once and the client pages through
  them locally; `CalendarController` filters the requested calendars down to the ones the viewer may
  see. No date-range querying.
- Event reminders are e-mails only (`ReminderMethods.Email`; the DB CHECK allows nothing else) and are delivered since
  2026-10-24 (owner's request): the domain works out each reminder's fire time when it is saved (`ReminderSchedule`: a timed
  event's start minus the lead time; an all-day event's day minus N days / weeks at its time of day in the calendar's zone;
  daylight-saving gaps move forward, overlaps take the earlier instant) and stores it with `HandledAt`. One already past is
  handled at once (it would arrive late). The worker's `CalendarReminderScheduler` runs `CalendarReminderDispatchService` at
  start-up and every minute: each due reminder in its own transaction (`LockDueAsync`, `FOR UPDATE SKIP LOCKED`), e-mailed to the
  calendar's owner through the mail queue or skipped per `ReminderDeliveryPolicy` (owner gone / deleted / locked / unconfirmed /
  the demo account, or over an hour late), then marked handled; a queue failure rolls back and is retried next minute. Viewers
  of a shared calendar and visitors get no reminders and are not shown them (`IsOtherCalendarEventExists` sends `[]`). The mail
  is Korean and English together (an account stores no language). Editing an event recreates its reminders, so a moved event's
  reminder can be sent again for the new time. Changing a calendar's time zone reschedules the pending reminders of its all-day
  events in the new zone, in the same transaction (`CalendarEventReminder.Reschedule`, since 2026-11-02); handled ones stay as they are. An event holds at most
  `CalendarReminderPolicy.MaxRemindersPerEvent` (10) reminders (since 2026-11-02): each is a mail through the SMTP relay, and an
  unbounded list let one request queue tens of thousands; the page mirrors it from `#maxRemindersPerEvent` and adds no row past it.
  An event's dates lie between `CalendarEvent.MinYear` (2) and `MaxYear` (9998) (since 2026-11-02): around year 1 / 9999 the
  time-zone conversion, a reminder's lead time and the grid's exclusive end left DateTime's range and showed a "temporary error". `CalendarRecurrence` / `RecurrenceId` stay unused on purpose — do not drop the table.
- An all-day event's reminder is what its form offers, on the server too (`CalendarReminderPolicy.CheckAllDayReminder`, owner's decision,
  2026-11-09): a lead time in days or weeks, at a time of day on the 15-minute step. `ReminderSchedule` still computes the other shapes on
  its own; `CalendarEventReminder.Create` no longer lets a new one through.
- The worker's periodic jobs (`CalendarReminderScheduler`, `FileManagerUploadSweepScheduler`, and the loop of
  `CalendarReminderDispatchService`) catch with `when (ex is not OperationCanceledException)` (owner's decision, 2026-11-03). Do not
  raise that an `OperationCanceledException` not caused by shutdown could escape and stop the worker host, and do not change the
  filter to `when (!stoppingToken.IsCancellationRequested)`.
