# Rule: no business logic in `.cshtml` or `wwwroot/**/custom/**/site.js`

Summary: all business logic goes in `.cs` files under `Maroik.Core.*`
(DDD + Clean Architecture). `.cshtml` views and `wwwroot/*/custom/**/site.js`
keep only rendering and input handling. This rule always applies — it does not
need to be restated per task.

## Scope

Applies to every Razor view under `Maroik.Website/Views/**/*.cshtml` and every
`Maroik.Website/wwwroot/{admin,anonymous,user}/custom/**/site.js`.

## Where logic belongs (Clean Architecture layers)

| Concern | Home |
|---|---|
| Entities, invariants, value objects, **policies / business rules / allowed-value taxonomies** | `Maroik.Core.Domain` |
| Use-case orchestration, transactions, cross-aggregate rules, DTO mapping | `Maroik.Core.Service` |
| Interfaces, DTOs | `Maroik.Core.Contract` |
| Persistence, EF Core, queries | `Maroik.Core.Repository` |
| External systems (SMTP mail, file storage, ClamAV, RabbitMQ…) | `Maroik.Core.Client` |
| HTTP surface: model-bind → call a service → return a result | `Maroik.Website/Controllers` |
| HTML structure + server-injected data | `Maroik.Website/Views/**/*.cshtml` |
| DOM wiring, widget init, AJAX transport, notifications | `wwwroot/**/custom/**/site.js` |

The dependency rule holds: `Website` → `Service` → `Domain`; `Domain` depends on
nothing. Repositories/Clients are wired through `Contract` interfaces.

## What MUST NOT appear in `.cshtml`

- `@{ ... }` / `@functions` blocks that compute, validate, branch on domain state,
  aggregate, sort/filter collections, or format money/dates by business rule.
  The view receives a ready-to-render view model.
- Inline `<script>` containing anything beyond a one-line call into `site.js`.
- Hardcoded business constants (limits, thresholds, class lists, tax/rate tables).
  Expose them from a Domain **policy** and serialize into the page:
  `@Html.Hidden("maxDaysBeforeEvent", CalendarReminderPolicy.MaxDaysBeforeEvent)`
  `@Html.Hidden("incomeSubClassMap", JsonConvert.SerializeObject(IncomeClassPolicy.SubClassesByMainClass))`

## What MUST NOT appear in `site.js`

- Business rules or magic numbers. Read them from server-injected hidden fields
  that a Domain policy produced. Client checks are **UX mirrors only** — the
  server re-validates every value on every write. Keep the mirror comment:
  `// Authoritative in <Policy> (server); mirrored here for form UX only.`
- Aggregation / derivation of displayed values (totals, balances, breakdowns).
  The controller returns computed numbers; JS only draws them.
- HTML sanitization, allow-listing, or content parsing for stored user content —
  use `IHtmlContentSanitizerService` / `IHtmlParserService` server-side.
- Business decisions about persistence, permissions, or workflow state.

### `site.js` MAY contain

jQuery element caching, `.on(...)` event wiring, datepicker/tabs/Chart.js/summernote
init, `$.ajax` calls to controller endpoints, `toastr` messages, Bootstrap modal
show/hide, MvcGrid reload, double-submit guards, base64→blob rendering, and
building `<option>`/DOM from a server response.

## When asked to "move business logic to DDD .cs"

1. Identify the rule and its aggregate; add/extend a **policy or domain method**
   in `Maroik.Core.Domain` with unit tests in `Maroik.Core.Domain.Tests`.
2. Call it from the relevant `Maroik.Core.Service` service; add a `Contract`
   interface member + DTO if the shape changes.
3. Have the controller return the computed result / expose the policy via the
   view model.
4. In `.cshtml`, replace the logic with a render of the view-model value, or a
   `@Html.Hidden(...)` of the serialized policy.
5. In `site.js`, delete the duplicated rule; read the hidden field instead and
   leave only the UX mirror with its "authoritative on server" comment.
6. Build + run the affected `*.Tests` project.

## Current state (2026-09, `refactor-code` branch)

This migration is already substantially complete (commit `88941c57 feat: DDD +
Clean architecture`). Policies exist for Finance class taxonomies, calendar
reminders, fixed schedules, passwords, image upload, and culture. Treat new
findings as regressions to fix, not a fresh migration.
