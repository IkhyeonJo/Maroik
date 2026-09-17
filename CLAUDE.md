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

## Always write resx + unit tests alongside code

Whenever you add or change code, also write the matching pieces in the same change —
don't leave them for later or treat them as optional:

- **Resource (.resx) files.** Any user-facing string (controller messages, ViewModel
  labels/validation messages, etc.) goes through the resx pair for its class, mirroring
  the source path 1:1 under `Maroik.Website/Resources/...` — e.g.
  `Controllers/FooController.cs` → `Resources/Controllers/FooController.en-US.resx` +
  `FooController.ko-KR.resx`. New class needing localized strings ⇒ new resx pair, both
  cultures filled in (no missing `ko-KR` or `en-US` entries).
- **Unit tests.** Every `*` project has a matching `*.Tests` project
  (`Maroik.Core.Domain` → `Maroik.Core.Domain.Tests`, `Maroik.Core.Service` →
  `Maroik.Core.Service.Tests`, etc. — see the list under `ls -d */ | grep Tests`). New
  or changed behavior in a source project gets corresponding tests added in its `.Tests`
  project in the same change, not deferred.

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

## Repository tests

`Maroik.Core.Repository.Tests` runs entirely against a real PostgreSQL 17 database
(Testcontainers) — there is no EF Core InMemory provider. One container is shared by
the whole assembly (`Infrastructure/PostgresContainerFixture`, an xUnit v3 assembly
fixture); it loads the real production schema **and seed data** from
`Maroik.DB/PostgreSQL/SQL_Init_Script/Debugging/Init.sql` into a template database,
and each test class gets its own throwaway DB cloned from that template
(`DatabaseFixture` + `RepositoryTestBase`). Docker must be available (CI's
`ubuntu-latest` has it); test-collection parallelism is off (`xunit.runner.json`).

Because every test starts from the full seed (~19k rows), a test must not assume an
empty table: insert under keys unique to the test (`RepositoryTestBase.UniqueEmail`
/ `Unique`, or `EnsureAccountsAsync` / `EnsureAssetsAsync` / `InsertCalendarAsync` /
`InsertCalendarEventAsync` for FK parents) and scope "list/count" assertions to the
rows or account the test itself created. Real CHECK / FK / composite-PK behaviour is
now exercised (e.g. `FixedIncome_DepositDayMonth_check`,
`CalendarEventReminder_Method_check`, `Board_Type_check`).

## Architecture validation tests

The layering / ArchNet rules are enforced by NetArchTest-based `*ArchitectureTests`
classes in each `*.Tests` project's `Architecture/` folder, plus the solution-wide
`SolutionLayeringArchitectureTests` (in `Maroik.Core.Contract.Tests`) that validates
the `.csproj` project- and package-reference graph itself. They run under
`dotnet test` in CI, so a violation fails the build. Keep them green; extend them
when the layer model changes.

## TODO

- **Retire the RSA SHA-1 fallback.** `RsaService.Decrypt` / `Verify` still accept ciphertext
  and signatures produced under the legacy `RsaAlgorithm=Rsa` (SHA-1 OAEP) config — needed only
  while 24 h reset/confirm links minted before the SHA-256 switch may still be in inboxes, and
  while historical Board/Forum/Calendar HTML holds SHA-1-OAEP storage-path ciphertext. Once the
  stored content has been re-encrypted (or is known to predate nothing that matters), drop
  `_legacyHashAlgorithmName` and both fallback paths so only SHA-256 is ever honoured.
