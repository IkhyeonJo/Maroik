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

## Strict TDD — no test, no code

All new and changed code is written test-first. This is a hard rule, not a preference:

1. **Red.** Write the test that describes the behaviour *before* the production code. Run it and watch it fail, for the
   right reason (the behaviour is missing — not a compile error or a typo in the test).
2. **Green.** Write the least production code that makes it pass. Nothing the tests do not demand.
3. **Refactor.** Clean up with the tests green. Behaviour changes need a new failing test first.
4. **A bug fix starts with a regression test** that fails on the current code. Revert the fix and confirm the test fails again
   (a test that passes without the fix proves nothing).

Consequences that apply to every change — C# under `Maroik.Core.*` / `Maroik.Website` / `Maroik.Worker` /
`Maroik.FileStorage`, and `TypeScripts/**/site.ts`:

- **Production code that no test exercises does not stay.** If a line, branch, method, class or script has no test that would
  fail without it, either write that test now or delete the code. "Defensive" branches that cannot be reached are not
  a reason to keep untested code: prove them reachable with a test, or remove them.
- **Tests must be able to fail.** No tests that only assert "does not throw", that pass regardless of the code, or that mock away
  the thing under test. Assert the concrete result (value, status code, message, persisted row, request body, DOM).
- **Prefer real collaborators over mocks** where the behaviour is about PostgreSQL, RabbitMQ, SMTP or HTTP: use the
  Testcontainers / in-process fakes already in the test projects (see "Tests that need Docker").
- **Do not lower a gate to make a change pass.** CI enforces line >= 80 % / branch >= 65 %, the architecture tests, and that the
  generated `wwwroot/**/site.js` matches the `.ts`. A change that needs an exemption gets a discussion, not a quiet skip.
- **Done means the suite is green** (`dotnet test` for the touched projects, `npm test` for scripts), the build has zero warnings,
  and the tests for the change are in the same commit as the change.

When asked for a change and it is unclear what test would drive it, ask that question first — the answer is the specification.

## Always write resx + unit tests alongside code

Whenever you add or change code, also write the matching pieces in the same change —
don't leave them for later or treat them as optional:

- **Resource (.resx) files.** Any user-facing string (controller messages, ViewModel
  labels/validation messages, etc.) goes through the resx pair for its class, mirroring
  the source path 1:1 under `Maroik.Website/Resources/...` — e.g.
  `Controllers/FooController.cs` → `Resources/Controllers/FooController.en-US.resx` +
  `FooController.ko-KR.resx`. New class needing localized strings ⇒ new resx pair, both
  cultures filled in (no missing `ko-KR` or `en-US` entries). This is a server-side
  (`.cs`/`.cshtml`) concern only — `TypeScripts/**/site.ts` never calls a localizer or
  owns a resx; it only reads text the server already localized into the DOM (hidden
  inputs / data attributes a Razor view rendered via `IStringLocalizer`), so there is
  nothing for a `.ts` change to add here.
- **Unit tests.** Every `.csproj` source project has a matching `*.Tests` project
  (`Maroik.Core.Domain` → `Maroik.Core.Domain.Tests`, `Maroik.Core.Service` →
  `Maroik.Core.Service.Tests`, etc. — see the list under `ls -d */ | grep Tests`), **and**
  `Maroik.Website/TypeScripts/**/site.ts` has its own counterpart,
  `Maroik.Website/TypeScripts.Tests/` (Vitest + jsdom; no `.csproj`, not in `Maroik.sln`,
  run via `npm test` from `Maroik.Website/` — see `TypeScripts.Tests/README.md`). New or
  changed behavior in a source project **or in a `.ts` file** gets corresponding tests
  added in its `.Tests` project in the same change, not deferred.

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

## Tests that need Docker

Besides `Maroik.Core.Repository.Tests` (PostgreSQL) the following start throwaway containers with Testcontainers, so Docker must
be available (CI's `ubuntu-latest` has it): `Maroik.Core.PostgreSQL.Tests` (PostgreSQL 17, both init scripts — the EF model must match
the real schema), `Maroik.Website.Tests` and `Maroik.E2E.Tests` (PostgreSQL 17), `Maroik.Core.Client.Tests` and
`Maroik.Worker.Tests` (RabbitMQ 4; the mail path uses an in-process SMTP server, `FakeSmtpServer`). Prefer these real services over
mocks when a test is about how the code behaves against PostgreSQL / RabbitMQ.

## Architecture validation tests

The layering / ArchNet rules are enforced by NetArchTest-based `*ArchitectureTests`
classes in each `*.Tests` project's `Architecture/` folder, plus the solution-wide
`SolutionLayeringArchitectureTests` (in `Maroik.Core.Contract.Tests`) that validates
the `.csproj` project- and package-reference graph itself. They run under
`dotnet test` in CI, so a violation fails the build. Keep them green; extend them
when the layer model changes.

## Domain error messages must go through LocalizableError

Every `Maroik.Core.Domain` error is built via `Maroik.Core.Domain.Localization.LocalizableError`
(`.Validation`/`.Conflict`/`.Failure`) — **never** call the raw `ErrorOr.Error.Validation` /
`.Conflict` / `.Failure` / `.NotFound` / `.Forbidden` / `.Unexpected` factories directly outside
`LocalizableError.cs` itself. This applies even to a message with no runtime value to interpolate
(pass zero `args`) — a message that starts static and later gains an interpolated value must not be
able to silently regress to bypassing localization.

Why: a raw `Error.X(code, "some message")` bakes the description straight into `Error.Description`
with no composite-format template in `Error.Metadata`, so the Website's `IHtmlLocalizer` resx lookup
falls back to the baked (often English, sometimes value-specific) string instead of resolving a
`ko-KR` translation. This was the actual state of ~98 of 104 Domain error call sites, and for
`Money.Add`/`Subtract`'s currency-mismatch errors it was a live bug: a genuinely runtime currency
code was interpolated straight into the message, so no resx key could ever match it.

Enforced by `DomainArchitectureTests.Domain_ErrorMessages_MustGoThrough_LocalizableError`
(`Maroik.Core.Domain.Tests/Architecture/DomainArchitectureTests.cs`) — a NetArchTest custom rule that
inspects each type's IL for a direct call to a raw `ErrorOr.Error` factory method (a type-dependency
check doesn't work here since every Domain type legitimately depends on the unrelated `ErrorOr<T>`
return type). Keep it green; when a new error is added, add it via `LocalizableError`, and add the
matching resx entry — with a `{0}`/`{1}` composite-format key, not a baked value — to every controller
resx pair (`en-US` + `ko-KR`) that can surface it.
