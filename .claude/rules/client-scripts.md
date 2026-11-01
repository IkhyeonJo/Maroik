---
paths:
  - "Maroik.Website/TypeScripts/**"
  - "Maroik.Website/TypeScripts.Tests/**"
  - "Maroik.Website/wwwroot/**"
  - "Maroik.Website/package*.json"
  - "Maroik.Website/tsconfig*.json"
  - "Maroik.Website/.npmrc"
  - "**/*.cshtml"
  - "**/Program.cs"
  - "**/*Sanitizer*"
---

# Intended design: Client scripts

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- The front end is server-rendered Razor + jQuery + AdminLTE, chosen on purpose: for a CMS it is the simplest and
  fastest stack, has no build chain beyond `tsc`, and stays runnable for years. Do not propose replacing it with a SPA
  framework (React, Vue, Angular, Blazor, ...) or adding a bundler.
- The vendored client libraries (jQuery, Bootstrap, AdminLTE and its plugins) are not tracked for security patches. The
  client is never trusted: the strict CSP in `Maroik.Website/Program.cs` (`script-src 'self'`, no `'unsafe-inline'` /
  `'unsafe-eval'`) blocks injected script, `HtmlContentSanitizerService` strips the `data-*` attributes library gadgets
  read, and the server re-validates every write. Do not raise an outdated client library or its CVEs as a finding.
- No bundler, no `import`/`export`: every `site.ts` is an IIFE global script compiled 1:1.
- `wwwroot/{admin,user,anonymous}` each keep the full vendored AdminLTE plugin set, including plugins no view references
  (datatables, pdfmake, codemirror, jszip, dropzone, flot, raphael, ...). Do not prune them.
- Each role (`admin` / `user` / `anonymous`) keeps its own script per view, even when two scripts are
  nearly identical. Do not merge them into shared scripts; the only shared helpers are the `window`
  globals each role's `_Layout` script defines.
- No `any` and no type assertion (`x as T`, `x!`; `as const` aside) in `TypeScripts/**` or `TypeScripts.Tests/**`
  (pinned by `TypeScripts.Tests/_common/noExplicitAny.test.ts` / `noImplicitAny.test.ts`), with `strict` plus
  `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes` and the other strict options in both tsconfigs. A value
  the code cannot prove goes through the runtime-check toolkit each role's `_Layout` script puts on `window`
  (`fieldValue`, `parseJson`, `byId`, `required`, `onReply`, … — see `TypeScripts/README.md`): a missing element
  throws, and an ajax reply that does not pass its check shows the generic "temporary error" toast. Each page
  declares its reply checks to mirror the controller's `Json(...)` result field for field, so a controller's reply
  change updates the page's check in the same change. `global.d.ts` is type-only (no runtime code).
