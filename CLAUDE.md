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

## Aggregate model

Every domain type persisted independently through its own `I*Repository`
(`IGenericRepository<T>` — its own `CreateAsync`/`UpdateEntityAsync`, not a cascade
through a parent) is an `AggregateRoot<TId>`, never a bare `Entity<TId>`. This
includes `BoardComment`, `BoardAttachedFile`, `CalendarEventReminder`,
`CalendarEventAttachedFile`, `CalendarShared` and `OtherCalendar` — reclassified
from `Entity<T>` to `AggregateRoot<T>` (base class only; parent-reference fields
are plain IDs, not object references), the same criterion applied to
`Category`/`SubCategory` in commit `284dc52f`. Enforced by
`ContractArchitectureTests.EveryRepositoryInterface_MustPersist_AnAggregateRoot`.

## Architecture validation tests

The layering / ArchNet rules are enforced by NetArchTest-based `*ArchitectureTests`
classes in each `*.Tests` project's `Architecture/` folder, plus the solution-wide
`SolutionLayeringArchitectureTests` (in `Maroik.Core.Contract.Tests`) that validates
the `.csproj` project- and package-reference graph itself. They run under
`dotnet test` in CI, so a violation fails the build. Keep them green; extend them
when the layer model changes.

## TODO

_(none)_
