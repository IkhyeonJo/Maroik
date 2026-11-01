---
paths:
  - "Maroik.Core.Repository/**"
  - "Maroik.Core.Repository.Tests/**"
  - "Maroik.DB/**"
  - "Maroik.Core.PostgreSQL/**"
  - "Maroik.Core.PostgreSQL.Tests/**"
---

# Repository tests

`Maroik.Core.Repository.Tests` runs entirely against a real PostgreSQL 17 database
(Testcontainers) — there is no EF Core InMemory provider. One container is shared by
the whole assembly (`Fixtures/PostgresContainerFixture`, an xUnit v3 assembly
fixture); it loads the real schema **and seed data** from
`Maroik.DB/PostgreSQL/SQL_Init_Script/Debugging/Init.sql` into a template database,
and each test class gets its own throwaway DB cloned from that template
(`DatabaseFixture` + `RepositoryTestBase`). Docker must be available;
test-collection parallelism is off (`xunit.runner.json`).

Because every test starts from the seed (menu/category rows plus the admin and demo accounts), a test must not assume an
empty table: insert under keys unique to the test (`RepositoryTestBase.UniqueEmail`
/ `Unique`, or `EnsureAccountsAsync` / `EnsureAssetsAsync` / `InsertCalendarAsync` /
`InsertCalendarEventAsync` for FK parents, and `EnsureNicknamesAsync` for a `Board` / `BoardComment`
`Writer`, which `Board_fk_0` / `BoardComment_fk_1` point at `Account.Nickname`) and scope "list/count" assertions to the
rows or account the test itself created. Real CHECK / FK / composite-PK behaviour is
now exercised (e.g. `FixedIncome_DepositDayMonth_check`,
`CalendarEventReminder_Method_check`, `Board_Type_check`).
