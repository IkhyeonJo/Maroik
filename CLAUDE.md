# Maroik — project instructions

## Architecture: DDD + Clean Architecture

Business logic lives **only** in `.cs` under `Maroik.Core.*`, never in
`.cshtml` views or the custom client scripts. Views render a ready-made
view model; the client scripts do DOM wiring, widgets, and AJAX transport only.
Client-side rule checks are UX mirrors — the server always re-validates.

The custom client scripts are authored in **TypeScript** at
`Maroik.Website/TypeScripts/**/custom/**/site.ts` and compiled 1:1 by `tsc` to
`Maroik.Website/wwwroot/**/custom/**/site.js` — **that `.js` is generated; edit the
`.ts`** (see `Maroik.Website/TypeScripts/README.md`).

Layer map (dependency direction: Website → Service → Domain; Domain depends on nothing):

- `Maroik.Core.Domain` — entities, invariants, value objects, **policies / business rules**
- `Maroik.Core.Service` — use-case orchestration, transactions, DTO mapping
- `Maroik.Core.Contract` — interfaces + DTOs
- `Maroik.Core.Repository` — EF Core persistence
- `Maroik.Core.Client` — external systems (SMTP mail, file storage, ClamAV, RabbitMQ)
- `Maroik.Website` — controllers (HTTP surface), views, static assets

Full rule, including "what must not appear where" and the step-by-step for moving
logic into the Core layers:

@.claude/rules/frontend-no-business-logic.md

## Branches

- `main` — production. No test suite / CI, but carries real security fixes.
- `refactor-code` — the DDD + Clean Architecture rewrite (current work).

## TODO

- **Reclassify 6 entities from `Entity<T>` to `AggregateRoot<T>`**: `BoardComment`,
  `BoardAttachedFile`, `CalendarEventReminder`, `CalendarEventAttachedFile`,
  `CalendarShared`, `OtherCalendar`. Each is persisted independently via its own
  repository (not hydrated/cascaded through a parent aggregate), which is exactly
  the criterion already used to reclassify `Category`/`SubCategory` as
  `AggregateRoot<long>` (see `Maroik.Core.Domain/Menu/Category.cs` and
  `SubCategory.cs` for the pattern, and commit `284dc52f` for that reclassification).
  This is a mechanical, low-risk change (base class only — parent-reference fields
  already use plain IDs, not object references). Left unfinished after commit
  `38d66bfe` removed the nine dead aggregate-invariant methods (`Board.DeleteComment`/
  `AttachFile`, `CalendarEvent.AddReminder`/`RemoveReminder`/`AttachFile`/`RemoveFile`,
  `Calendar.AddSubscriber`/`RemoveSubscriber`/`UpdateSharing`) that had falsely implied
  these were child-managed entities.
