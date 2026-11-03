---
paths:
  - "**/*.cs"
  - "**/LogTemplates*"
  - "**/appsettings*.json"
---

# Logging — written and tested with the code, like TDD

Logging is part of the behavior, not decoration added afterward. Every new or changed use case, endpoint, worker step or
client call ships with its logging **and the test that proves it**, in the same change. The order is the TDD order:

1. **Red.** Before the production code, write the test that asserts the log entry (level, message template / rendered text,
   whether an exception is attached, and — where relevant — that no secret is present). Use a recording `ILogger<T>` (see
   `RecordingLogger` in `Maroik.Core.Client.Tests/Clients/FileClientTests.cs`) and watch it fail because the log is missing.
2. **Green.** Add the smallest logging that passes.
3. **A log call no test would notice disappearing does not stay.** Remove the log call and confirm the test fails.

What must be logged:

- **Every `catch` either logs or rethrows.** A swallowed exception with no log is a defect. The only allowed silent catches are
  deliberate, commented, and tested translations of an *expected* condition into a result (e.g. a PostgreSQL unique-violation
  turned into `Conflict`, `OperationCanceledException` on shutdown, a health check returning its own `HealthCheckResult`).
  Everything else logs with the exception object as the first argument (`logger.LogError(ex, "… {Id}", id)`), never
  `ex.Message` alone.
- **Security and audit events are logged at `Information`/`Warning`**, whether they succeed or fail: login success / failure /
  lockout, registration, email confirmation, password-reset request and completion, forced password change, role / lock /
  delete changes made by an admin, permission denials, and rejected uploads. The log must let an operator answer "who did what to
  which account, and when" without reading the database.
- **Every failed write use case** logs why it failed (a result of `Failure`/`Conflict`/`Validation` produced by an unexpected
  condition is logged; a plain user-input validation error is not an event).
- **External calls** (SMTP, RabbitMQ, file storage, ClamAV) log a failure with the target, and a degraded-but-continuing
  path (fallback, retry, dropped item) logs a `Warning`.

Levels: `Error` = unexpected failure someone must look at; `Warning` = abnormal but handled (hostile-looking input, degraded
dependency, retry); `Information` = business / audit events; `Debug`/`Trace` = diagnostics only, off in production.

How:

- **Structured templates with named placeholders** (`"Locked account {Email} after {Attempts} attempts"`), never string
  interpolation or concatenation, so the values stay queryable and cannot forge extra log lines. The console output templates
  (`Maroik.Website/Constants/LogTemplates.cs`, `Maroik.FileStorage/Constants/LogTemplates.cs`, `WorkerHost.ConsoleOutputTemplate`)
  render `{Message:j}`, not Serilog's usual `:lj` — `:l` writes a string value raw, so a line break inside it would start a
  forged line. Keep `:j`; each host has a test that fails if a value can break the line.
- **Never log secrets or content:** passwords, password hashes, registration / reset tokens or their protected (Data Protection) form, session
  or antiforgery values, connection strings, mail bodies, uploaded file bytes, post/comment text. Identify an account by the
  identifier the existing logs already use (its email). A test asserts a secret does not appear when one could leak.
- **Layering:** `Maroik.Core.Domain` never logs (it stays pure — it returns errors); `Maroik.Core.Repository` does not log (the
  exception propagates to the service that owns the transaction and logs there). Services, clients, workers, controllers and
  filters log. Do not log-and-rethrow the same exception at several layers — log it once, where it is handled.
- **Done also means** every `catch` you added or touched satisfies the rules above and its test is green.

Enforced by tests where a syntax tree can decide (since 2026-10-20, at the owner's request): `LoggingRuleArchitectureTests`
(`Maroik.Core.Contract.Tests`) reads every production source with the Roslyn parser (the PostgreSQL scaffold aside) and fails when
- a `catch` neither logs, nor throws, nor carries a comment (in its block or just before `catch`) saying why it may be silent —
  so an allowed silent catch (an expected condition turned into a result) always states its reason;
- a silent `catch` swallows every exception: no type or `Exception` itself, no `when` filter, and the exception is not handed on
  (a health check's `HealthCheckResult.Unhealthy(..., ex)` hands it on). A comment does not excuse that;
- an `Error` / `Critical` (Serilog `Error` / `Fatal`) log inside a `catch` that names its exception does not pass that exception
  first (judged against the innermost enclosing `catch`);
- a log call passes the caught exception's `.Message`;
- a template placeholder's name ends in a secret noun (`Password`, `Token`, `Secret`, `Hash`, `ConnectionString`, `Cookie`);
  `{PasswordReset}` (whether a reset happened) passes;
- a value passed to a log is a variable or member whose name ends in one of those nouns (`account.HashedPassword`) — this
  reads names only, so a secret passed under an innocent name is still the reviewer's to catch;
- `Maroik.Core.Repository` mentions `ILogger` / `ILoggerFactory` / Serilog `Log`.
`TheRules_FireOnWhatTheyForbid_AndOnlyOnThat` pins each rule against a bad and a good sample. Whether a use case logs at all, at
which level and with what text stays with its own tests (the TDD order above). `DomainArchitectureTests.Domain_ShouldNot_Log`
keeps `Maroik.Core.Domain` off `Microsoft.Extensions.Logging`.

The compiler does enforce the template rules (`.editorconfig`, "Logging templates" block, all `error`): CA2254 (the message
template is a constant — no interpolation or concatenation), CA2017 (its `{Placeholders}` match the arguments) and CA1727
(placeholders are PascalCase).
