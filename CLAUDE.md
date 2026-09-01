# Maroik — project instructions

## Architecture: DDD + Clean Architecture

Business logic lives **only** in `.cs` under `Maroik.Core.*`, never in
`.cshtml` views or `wwwroot/**/custom/**/site.js`. Views render a ready-made
view model; `site.js` does DOM wiring, widgets, and AJAX transport only.
Client-side rule checks are UX mirrors — the server always re-validates.

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
