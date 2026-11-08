---
paths:
  - "Maroik.Core.Service/**"
  - "Maroik.Core.Service.Tests/**"
  - "Maroik.Core.Repository/**"
  - "Maroik.Core.Repository.Tests/**"
  - "Maroik.Core.Contract/Interfaces/**"
  - "**/*UnitOfWork*"
  - "**/*Repository*"
---

# Intended design: Data access and transactions

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- Every service write that runs in a transaction (several statements that must succeed together, or a `FOR UPDATE`
  lock) spells out its own `BeginAsync` / `CommitAsync` / `RollbackAsync` (and the service tests assert them). Do not
  fold them into a decorator, filter or "unit of work per request" helper. A single column-scoped write runs on its own
  (`ProfileService.UpdateTimeZoneAsync`, the fixed income / expenditure CRUD), and so do the mail flows that persist a
  token, send, then record the outcome (`AccountPasswordResetService.ForgotPasswordAsync`): a transaction around the send would hold
  a row lock across the network call.
- Several rows are locked in one statement, `... ORDER BY <column> FOR UPDATE` without `LIMIT` (e.g. the asset
  balance lock). This is the pattern that fixed a lost-update bug; do not report it as a deadlock-ordering
  risk. (`LIMIT` combined with `ORDER BY ... FOR UPDATE` would be a different, documented caveat.)
- Posts and comments are owned by `Nickname`, not by email: a nickname is unique and never changes after
  registration (only an unconfirmed account's can, via `ReplaceUnconfirmedRegistration`), and accounts are
  only soft-deleted.
