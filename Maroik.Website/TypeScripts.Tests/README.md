# TypeScripts.Tests/ — client-script tests

## Prerequisites — Node.js (written for a non-JS dev)

This suite has **no `.csproj`**, is **not** its own npm package, and is not in
`Maroik.sln`. Vitest, jsdom, jQuery and the `typescript` compiler are npm
**devDependencies of `Maroik.Website/package.json`**, and every command below is
run from **`Maroik.Website/`** (the folder with `package.json` — *not* the
capital-**S** `Maroik.WebSite/` at the repo root). Nothing here runs without
Node.js on the machine.

**Full install walkthrough — with expected output for each step — is in
[`../TypeScripts/README.md`](../TypeScripts/README.md) ("Prerequisites").** The
short version:

1. **Install Node.js 22** (LTS). The build uses Node 22 everywhere
   (`Maroik.Website/Dockerfile` → `FROM node:22-alpine`; CI doesn't use Node).
   `package.json` `"engines"` requires `node >=22.22.2 <23` and
   `Maroik.Website/.npmrc` has `engine-strict=true`, so any other version makes
   the next step stop with `npm error code EBADENGINE` (nothing installed — just
   switch Node and retry). On Windows, install the **LTS** build from
   <https://nodejs.org>, then open a **new terminal**. Check: `node -v` prints
   `v22.22.2` or newer 22.x.

2. **`npm ci`** from `Maroik.Website/` — installs ~105 packages into
   `node_modules/` (git-ignored). This **one** install covers both `TypeScripts/`
   and this test tree. Success ends with `found 0 vulnerabilities`.

3. **`npm test`** from `Maroik.Website/` — see **Running** below.

`TypeScripts.Tests/tsconfig.json` is standalone for **editor / `tsc --noEmit`
type-checking only** — it emits nothing and needs no separate install.

If you never run `npm ci`, `dotnet build` still succeeds — with `node_modules/`
absent the MSBuild target that would run the suite is a silent no-op (see
**Running → In a build**), so a missing Node install is quiet, not a build error.

---

Vitest + jsdom tests for the custom client scripts. They run the **compiled**
`wwwroot/{role}/custom/{Feature}/{Page}/js/site.js` (not the `.ts`) inside a jsdom
window against a minimal DOM fixture, so they exercise the exact script that ships.

See `../TypeScripts/README.md` for the migration itself.

---

## Layout — a 1:1 mirror of `wwwroot`

```
TypeScripts.Tests/
  _common/
    harness.ts            loadSite() + fixture helpers + jsdom stubs   (test infra, no source counterpart)
    vitest.setup.ts       runs once before each test file
  {role}/custom/{Feature}/{Page}/js/site.test.ts   ⟷  wwwroot/{role}/custom/{Feature}/{Page}/js/site.js
  tsconfig.json           standalone (the main tsconfig excludes this tree)
```

**Every `site.js` has exactly one `site.test.ts` at the matching path**, and the only
files that don't mirror a source path are under `_common/`. Each test file inlines its
own DOM fixture — there are deliberately **no** cross-role shared fixture/spec files
(so the tree stays a pure mirror; the small amount of fixture duplication is the price).

Imports use the **`@tests/*` alias** (`== TypeScripts.Tests/*`), so a helper import
reads the same from any depth. It's declared twice, keep them in sync:

* `../vitest.config.mts` → `resolve.alias` (runtime)
* `./tsconfig.json` → `baseUrl` + `paths: { "@tests/*": ["*"] }` (editor / type-check)

---

## The harness (`_common/harness.ts`)

`loadSite(role, feature, page, fixtureHtml, opts?)`:

1. resets `document.body` to `fixtureHtml`,
2. puts a **real jQuery 3.6.0** on `window` (matching the runtime) + chainable no-op
   stubs for every vendored plugin the scripts call — `tabs` / `modal` / `tooltip` /
   `datepicker` / `summernote` / `valid` — and stubs the `toastr`, `MvcGrid`,
   `FullCalendar`, `moment`, `Chart` globals and `$.datepicker`,
3. captures `$.ajax` calls (`h.ajaxCalls`, `h.lastAjax()`, `h.respond()`),
4. patches the browser bits jsdom lacks or refuses: `window.location.href`
   (→ `h.navigations`), `HTMLFormElement.submit` (→ `h.submittedForms`), `alert`,
   `confirm`, `URL.createObjectURL`,
5. **fake-timer-flushes** the eval — jQuery defers `$(fn)` via `setTimeout` when the
   document is already `complete` (it is, in jsdom), so the harness wraps the eval in
   `vi.useFakeTimers()` + `vi.runOnlyPendingTimers()` to run those ready-callbacks
   (where most Calendar/Forum wiring lives) synchronously before the test asserts,
6. `window.eval`s the compiled `site.js` (it self-executes — it's the IIFE-wrapped port).

Returned `SiteHandle`: `$`, `win`, `ajaxCalls` / `respond` / `lastAjax`, `toastr`,
`charts`, `submittedForms`, `navigations`, `MvcGridInstances`.
Also exported: `hidden(id, value)`, `antiForgery`, `hiddenByStyle(el)`,
`fireNative(target, type, detail)`.

### jsdom gotchas the tests work around

* **no layout** → `:visible` / `:hidden` are always false/true. Use
  `hiddenByStyle(el)` (reads `style.display`), not jQuery pseudo-selectors.
* **MvcGrid events** are native `CustomEvent`s (`el.dispatchEvent`), not
  `$.trigger` — use `fireNative(document, "rowclick", { data: { … } })`.
* mutating `$.fn.*` in a test leaks within the file; the harness re-assigns the
  stubs on every `loadSite`, so call it fresh per test.

---

## What is tested

"The parts worth locking down per script", not every handler:

* **server-policy UX mirrors** — income/expenditure sub-class filtering
  (`ApplyAllowedOptions`), fixed-schedule deposit-day limits (`DepositDayValues`),
  the calendar reminder lead-time bounds (`Validate*` vs `CalendarReminderPolicy`),
* **form / request wiring** — `ExportExcel*` builds the right POST form; create/edit
  forms hit the right URL with the right JSON / `FormData`; an invalid form makes no
  request,
* **small behaviours** — culture switch, grid row highlight, date localisation, the
  `_Layout` double-submit guard,
* a **smoke `loadSite()`** for the 2,300- / 2,700-line Calendar scripts (proves the
  FullCalendar / moment / datepicker / summernote stubs are enough to run them).

22 test files, 58 tests.

## Adding a test

1. `TypeScripts.Tests/{role}/custom/{Feature}/{Page}/js/site.test.ts` (mirror the source).
2. `import { loadSite, … } from "@tests/_common/harness";`
3. Build a fixture with just the elements the assertion needs (`antiForgery + hidden(…) + \`<html>\``).
4. `const h = loadSite("{role}", "{Feature}", "{Page}", fixture);` then trigger events / drive
   `h.respond(0, { result: true, … })` and assert on `h.ajaxCalls` / `h.navigations` /
   `h.submittedForms` / `h.toastr` / the DOM.

---

## Running

```bash
npm test          # from Maroik.Website/ — runs `npm run build` (pretest) then `vitest run`
npm run test:watch
npx vitest run TypeScripts.Tests/user/custom/AccountBook   # a subset
```

`npm test` recompiles the `.ts` first (the `pretest` hook) because the suite runs
the **compiled** `site.js`, then runs Vitest. A successful run ends with:

```
 Test Files  22 passed (22)
      Tests  58 passed (58)
```

Expect ~30 lines of `Not implemented: HTMLCanvasElement's getContext() ...` to
scroll past on the way — that is **not a failure**, just jsdom reporting it has
no `<canvas>` engine for the Chart.js code to use. Only the
`Test Files … passed` / `Tests … passed` summary matters; a real failure prints
`FAIL` and names the test file.

**In a build:**

* **`dotnet build` / `dotnet test` / Rider / VS** — the `RunClientScriptTests` target
  (`Maroik.Website.csproj`, `AfterTargets="Build"`) runs `npx vitest run` **on by
  default** wherever `npm ci` has been done. It is **incremental** — skipped unless a
  `TypeScripts/**/*.ts`, `TypeScripts.Tests/**/*.ts`, `tsconfig.json`,
  `vitest.config.mts` or `package.json` changed since the last pass (stamp:
  `obj/…clientscripttests.stamp`), so a C#-only edit + build doesn't pay for it.
  One-off fast build: `dotnet build -p:RunClientScriptTests=false`.
* **Docker** — the `Dockerfile` `tsbuild` stage runs the suite only when built with
  `--build-arg RUN_CLIENT_TESTS=true` (keeps Rider/VS "regular mode" docker-compose
  debug from being gated on it). CI passes that arg.

Not a .NET project (no `.csproj`) — not in `Maroik.sln`.
