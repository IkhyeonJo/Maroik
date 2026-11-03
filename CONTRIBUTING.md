# Contributing to Maroik

Most of this repository's rules are enforced by the build and the tests, so a change that builds with zero warnings and
keeps every suite green already follows them. This page lists what you need to get there, and where the rest is written
down.

## Prerequisites

- The .NET SDK pinned in `global.json` (exact version, `rollForward: disable`).
- Node 22 (`Maroik.Website/package.json` `engines`, enforced by `.npmrc`) for the TypeScript client scripts.
- Docker, for the test projects that start PostgreSQL 17 / RabbitMQ 4 containers (Testcontainers) and for the E2E suite.

## The definition of done

```bash
cd Maroik.Website && npm ci && npm test && cd ..          # tsc + Vitest (client scripts)
dotnet build Maroik.slnx                                   # analyzers are errors: zero warnings
dotnet test --solution Maroik.slnx --max-parallel-test-modules 1
```

Run the test modules one at a time: the E2E browser fails when another suite's container leaves the Docker bridge.
CI (`.github/workflows/ci-cd.yml`, on pull requests into `main`) runs the same steps plus a coverage gate
(line >= 80 %, branch >= 65 %).

## What the build and the tests already check for you

| Rule | Checked by |
|---|---|
| Layering (Website → Service → Domain; Domain depends on nothing), project graph, `Maroik.slnx` registration | `*ArchitectureTests`, `SolutionLayeringArchitectureTests` |
| Disposal, security, culture, banned APIs (clock, blocking waits, `Console`), unnecessary usings | `.editorconfig` rule IDs (errors), `BannedSymbols.txt` |
| Logging: every `catch` logs or rethrows, no secrets in templates, structured templates | `LoggingRuleArchitectureTests`, CA2254 / CA2017 / CA1727 |
| Plural folder names, one type per file, folder roles (`Interfaces/`, `Extensions/`, `Fixtures/`, …) | `MAROIK0001`, SA1402 / SA1649, `FolderRoleArchitectureTests` |
| Domain errors through `DomainError`, service error keys from `ServiceErrorKeys`, resx completeness | `DomainArchitectureTests`, `ServiceErrorKeyArchitectureTests`, `ServiceErrorKeyResxTests`, `ResourceCompletenessTests` |
| No `any` / type assertions in the client scripts | `noExplicitAny.test.ts`, `noImplicitAny.test.ts` |
| Every POST action has a failure-contract test | `ControllerFailureContractTests` |

## What is left to you

- **Test first.** New and changed behavior starts with a failing test, and a bug fix with a regression test that fails
  on the current code (see "Strict TDD" in `CLAUDE.md`).
- **Business logic lives in `Maroik.Core.*`**, never in `.cshtml` views or `site.ts` (`.claude/rules/frontend-no-business-logic.md`).
- **Edit the `.ts`, never the generated `wwwroot/**/site.js`.**
- **Both cultures** (`en-US`, `ko-KR`) for every user-facing string.
- **Read the intended design of the area you touch** before "fixing" something that looks like a gap: much of what looks
  like a smell is deliberate. `CLAUDE.md` holds the cross-cutting items and lists one file per area under `.claude/rules/`
  (accounts, calendar, file manager, finance, budgets, …).

`CLAUDE.md` and `.claude/rules/` are the full rule set; the files under `.claude/rules/` load automatically for Claude Code
when a file of their area is opened, and are plain Markdown for everyone else.
