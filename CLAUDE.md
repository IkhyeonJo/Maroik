# Maroik — project instructions

## Architecture: DDD + Clean Architecture

Business logic lives **only** in `.cs` under `Maroik.Core.*`, never in
`.cshtml` views or the custom client scripts. Views render a ready-made
view model; the client scripts do DOM wiring, widgets, and AJAX transport only.
Client-side rule checks are UX mirrors — the server always re-validates.

The custom client scripts are authored in **TypeScript** at
`Maroik.Website/TypeScripts/**/custom/**/site.ts` and compiled 1:1 by `tsc` to
`Maroik.Website/wwwroot/**/custom/**/site.js` — **that `.js` is generated and git-ignored; edit the
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

## Intended design — do not "fix" these

The items below look like gaps or smells but are deliberate. Do not change them, add guards around
them, or refactor them away unless the owner explicitly asks for that specific change.

Deployment and schema
- A code-only deployment (`AutoDeploy.sh`) replaces the app containers and keeps the database. A commit that changes
  `Maroik.DB/` waits for the manual redeploy, which removes every Docker volume and recreates the database from `Init.sql`
  (README, "Redeploy after a schema change"). There are no production migrations, no data back-fills and no
  backward-compatibility concerns. A schema change means editing `Init.sql` (every variant) and the EF Core model so they
  match — nothing else.
- Judge code against the schema, the seed scripts and the tests only. The production data is the owner's
  responsibility: do not raise checks against existing production rows, ask for queries to be run against it, or
  add safeguards for hypothetical legacy rows.
- `Maroik.Core.PostgreSQL` (`Contexts/ApplicationDbContext`, `Models/*`) is EF Core scaffold output. Leave it out of
  repo-wide comment / style passes; touch it only when the schema mapping itself changes.

Code and tests
- The DDD + Clean Architecture layering is deliberate preparation for growth, not over-engineering.
- Long rationale comments are wanted; do not trim them in a review.
- Service tests mock the repositories, and the Service layer's mutation score sits below the Domain's on purpose:
  the business rules live in Domain (with its own tests), and real-database coverage comes from the Repository
  and Website integration tests.
- A service reachable only on the internal Docker network gets no app-level authentication (API keys, shared
  secrets, JWT) and no `IsDevelopment()` gating: the network boundary is its security boundary.

The other areas of "Intended design" each live in their own file under `.claude/rules/`, which Claude Code loads
when a file of that area is read or edited (the `paths:` globs at the top of each file). They are as binding as the
items above:

- Operations and hosting — `.claude/rules/operations-and-hosting.md`
- Authorization and navigation — `.claude/rules/authorization-and-navigation.md`
- Accounts — `.claude/rules/accounts.md`
- Data access and transactions — `.claude/rules/data-access-and-transactions.md`
- Finance — `.claude/rules/finance.md`
- Files and content — `.claude/rules/files-and-content.md`
- Calendar — `.claude/rules/calendar.md`
- Kanban — `.claude/rules/kanban.md`
- File Manager — `.claude/rules/file-manager.md`
- Budgets (Income Budget / Expenditure Budget) — `.claude/rules/budgets.md`
- Client scripts — `.claude/rules/client-scripts.md`
- Error handling — `.claude/rules/error-handling.md`

A new feature area gets its own rule file with `paths:` globs for its files, named in this list.
`ClaudeRulesArchitectureTests` (`Maroik.Core.Contract.Tests`) fails when a glob matches no file of the repository (a
renamed or removed folder), when a rule file is neither path-scoped nor imported, and when this list and the folder
disagree.

## Reviewing this repo

- **Read "Intended design" before starting any review** — the items above and every `.claude/rules/` file of an
  area the review touches (a whole-repo review reads them all) — and check every candidate finding against it before
  reporting.
- **Intended design and the tests decide.** A behavior listed there, or pinned by a test, is intended — leave it and do
  not raise it. Anything else is judged on its own merits; when it is unclear whether a behavior is deliberate, look for
  the reason first (`git log -S` / `git blame`, the rationale comments) and ask before calling it a bug.
- Items listed under "Intended design" stay closed; re-raise one only if its premise changes (for example
  `Maroik.FileStorage` becomes reachable from outside the internal network).

## Strict TDD — no test, no code

All new and changed code is written test-first. This is a hard rule, not a preference:

1. **Red.** Write the test that describes the behavior *before* the production code. Run it and watch it fail, for the
   right reason (the behavior is missing — not a compile error or a typo in the test).
2. **Green.** Write the least production code that makes it pass. Nothing the tests do not demand.
3. **Refactor.** Clean up with the tests green. Behavior changes need a new failing test first.
4. **A bug fix starts with a regression test** that fails on the current code. Revert the fix and confirm the test fails again
   (a test that passes without the fix proves nothing).

Consequences that apply to every change — C# under `Maroik.Core.*` / `Maroik.Website` / `Maroik.Worker` /
`Maroik.FileStorage`, and `TypeScripts/**/site.ts`:

- **Production code that no test exercises does not stay.** If a line, branch, method, class or script has no test that would
  fail without it, either write that test now or delete the code. "Defensive" branches that cannot be reached are not
  a reason to keep untested code: prove them reachable with a test, or remove them.
- **Tests must be able to fail.** No tests that only assert "does not throw", that pass regardless of the code, or that mock away
  the thing under test. Assert the concrete result (value, status code, message, persisted row, request body, DOM).
- **Prefer real collaborators over mocks** where the behavior is about PostgreSQL, RabbitMQ, SMTP or HTTP: use the
  Testcontainers / in-process fakes already in the test projects (see "Tests that need Docker").
- **Do not lower a gate to make a change pass.** The bar is line >= 80 % / branch >= 65 % coverage, the architecture tests, and the
  client-script tests against freshly compiled `wwwroot/**/site.js`. A change that needs an exemption gets a discussion, not a quiet skip.
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
  `Maroik.Website/TypeScripts.Tests/` (Vitest + jsdom; no `.csproj`, not in `Maroik.slnx`,
  run via `npm test` from `Maroik.Website/` — see `TypeScripts.Tests/README.md`). New or
  changed behavior in a source project **or in a `.ts` file** gets corresponding tests
  added in its `.Tests` project in the same change, not deferred.

## Tests that need Docker

Besides `Maroik.Core.Repository.Tests` (PostgreSQL) the following start throwaway containers with Testcontainers, so Docker must
be available: `Maroik.Core.PostgreSQL.Tests` (PostgreSQL 17, the init script — the EF model must match
the real schema), `Maroik.Website.Tests` and `Maroik.E2E.Tests` (PostgreSQL 17), `Maroik.Core.Client.Tests` and
`Maroik.Worker.Tests` (RabbitMQ 4; the mail path uses an in-process SMTP server, `FakeSmtpServer`). Prefer these real services over
mocks when a test is about how the code behaves against PostgreSQL / RabbitMQ.

`Maroik.E2E.Tests` must not run alongside the other Testcontainers suites: when another session's container leaves the Docker
bridge, the browser aborts a request in flight (`net::ERR_NETWORK_CHANGED`), and `OtherTestcontainersTests` expects no other
session to be running. A solution-wide run must therefore run the modules one at a time:

    dotnet test --solution Maroik.slnx --max-parallel-test-modules 1

## Architecture validation tests

The layering / ArchNet rules are enforced by NetArchTest-based `*ArchitectureTests`
classes in each `*.Tests` project's `ArchitectureRules/` folder, plus the solution-wide
`SolutionLayeringArchitectureTests` (in `Maroik.Core.Contract.Tests`) that validates
the `.csproj` project- and package-reference graph itself. They run under
`dotnet test`, so a violation fails the test run. Keep them green; extend them
when the layer model changes.

**Every project — including a new one — must be registered in `Maroik.slnx` under the matching solution folder** (`src`
for shipped code, `tests` for `*.Tests`): `dotnet sln Maroik.slnx add <csproj> --solution-folder tests`. Enforced by
`SolutionLayeringArchitectureTests` (`EveryProject_MustBeRegisteredInTheSolution`,
`EveryProject_MustBeNestedUnderTheMatchingSolutionFolder`).

## Adding rules and checks

A new rule, architecture test or other check is added only after a real incident, or where data or security is at stake. A
check that only repeats a value of a settings file is not added. One found later, or a rule whose reason is gone, is removed.
The architecture tests of the code and the domain tests stay: they are worth what they cost.

## Rules kept in `.claude/rules/`

These sections apply to the C# code, the build or the tests, and load with the files they govern. Each keeps its
heading, so a comment that cites "CLAUDE.md, <section>" is found through this list:

- Logging — written and tested with the code, like TDD — `.claude/rules/logging.md`
- Memory safety — resource-lifetime analyzers are build errors (with its "Security", "String comparison and culture",
  "Banned APIs" and "One pinned SDK" parts) — `.claude/rules/analyzers-and-memory-safety.md`
- Folder names are plural nouns — `.claude/rules/csharp-code-structure.md`
- One type per file, named after it — `.claude/rules/csharp-code-structure.md`
- Aggregate model — `.claude/rules/csharp-code-structure.md`
- Domain error messages must go through DomainError — `.claude/rules/csharp-code-structure.md`
- Service error keys must come from ServiceErrorKeys — `.claude/rules/csharp-code-structure.md`
- Repository tests — `.claude/rules/repository-tests.md`
- Mutation testing (on demand) — `.claude/rules/mutation-testing.md`

