# TypeScripts/ — custom client scripts

## Prerequisites — install Node.js first

`tsc` is **not** a system-wide tool here. The compiler (`typescript@7.0.2`, the
native build), plus every `@types/*` package, are npm **devDependencies** of
`Maroik.Website/package.json` and only run through its `scripts`. With no Node
on the machine, nothing in this tree compiles — that is the whole reason a fresh
checkout "doesn't work".

1. **Install Node.js 22.x** — match the version the build already uses:
   `Maroik.Website/Dockerfile` compiles this tree in a `FROM node:22-alpine`
   stage (`tsbuild`), and the GitHub Actions CI doesn't run Node at all, so that
   line is the reference. `package.json` `engines` pins it as
   `node: ">=22.22.2 <23"` (the `22.22.2` floor is `jsdom@30`'s own requirement),
   and `Maroik.Website/.npmrc` sets `engine-strict=true` — so `npm ci` / `npm install`
   **fails fast with `EBADENGINE`** on any other Node. `npm` ships with it.
   * nvm: `nvm install 22 && nvm use 22`
   * or a package manager / the installer from <https://nodejs.org>
   * verify: `node -v` → `v22.22.2` or newer 22.x, and `npm -v` prints a version.
2. From **`Maroik.Website/`** (the folder with `package.json`):

   ```bash
   npm ci            # once after checkout — creates node_modules/ (tsc + @types/*)
   npm run build     # tsc -p tsconfig.json  → writes wwwroot/**/site.js
   npm run check     # tsc --noEmit  (type-check only)
   npm run watch     # rebuild on save
   ```

`TypeScripts.Tests/` is **not** a separate npm package — it shares this same
`package.json` / `node_modules/`, so the one `npm ci` above also sets up the
tests (`npm test`, see `../TypeScripts.Tests/README.md`).

Without `node_modules/`, `dotnet build` silently skips the TypeScript compile and
falls back to the committed `wwwroot/**/site.js` (see **Build integration** below),
so a missing Node install fails quietly rather than loudly.

---

The 22 hand-written client scripts under `wwwroot/{admin,anonymous,user}/custom/**/js/site.js`
are authored here as TypeScript and compiled back to the exact same paths.

```
Maroik.Website/
  TypeScripts/{area}/custom/{Feature}/{Page}/js/site.ts   ── source (this tree)
                │  tsc  (rootDir: TypeScripts, outDir: wwwroot)
                ▼
  wwwroot/{area}/custom/{Feature}/{Page}/js/site.js        ── generated, COMMITTED
```

No other `wwwroot` asset is touched. Vendored libraries (jQuery, AdminLTE, FullCalendar,
summernote, MvcGrid, …) stay exactly as they are and are **not** compiled or bundled.

---

## Code style

Whitespace / EOL / quotes / semicolons for `*.ts` are pinned in the **repo-root
`.editorconfig`** ("TypeScript / JavaScript" section) — standard keys plus the
`ij_typescript_*` / `ij_javascript_*` keys that are Rider's / WebStorm's code-style
mechanism (Rider reads JS/TS style straight from `.editorconfig`). `TypeScripts/**`
is narrowed there to **whitespace-only** rules so a reformat-on-save can't churn the
byte-faithful port; `TypeScripts.Tests/**` is 2-space; the `tsc` output
(`wwwroot/**/custom/**/site.js`) is `generated_code = true`. `Maroik.sln.DotSettings`
carries the few ReSharper-side JS/TS toggles that aren't editorconfig-backed
(e.g. TS interfaces take **no** `I` prefix, unlike C#).

## Tech stack

| Purpose | Tool | Version | Notes |
|---|---|---|---|
| Type-check **and** emit | **TypeScript** (`tsc`) | **7.0.2** | The native (Go) compiler — the whole `TypeScripts/` tree type-checks + emits in ~6 s. No Babel, no bundler. |
| Type-check-only gate | `tsc --noEmit` | — | `npm run check`; use in CI. |
| Tests | **Vitest** + **jsdom** + real **jQuery 3.6.0** | 4.1.11 / 30.0.1 / 3.6.0 | See `../TypeScripts.Tests/`. |

### `@types/*` — pinned to the runtime versions actually loaded by `Views/Shared/_Layout.cshtml`

| Loaded script (`wwwroot/{area}/…`) | Runtime version | Dev dependency |
|---|---|---|
| `plugins/jquery/jquery.min.js` | jQuery 3.6.0 | `@types/jquery@3.5.34` — the 3.5.x line targets jQuery 3.x; `@types/jquery@4` is for jQuery 4 and would be wrong. |
| `plugins/jquery-ui/jquery-ui.min.js` | jQuery UI 1.12.1 | `@types/jqueryui@1.12.24` |
| `lib/jquery-validation/dist/jquery.validate.min.js` | jquery-validation 1.17.0 | `@types/jquery.validation@1.17.1` |
| `plugins/bootstrap/js/bootstrap.bundle.min.js` | Bootstrap 4.6.0 | none — the scripts only call `$el.modal(...)`, hand-typed in `global.d.ts`. `@types/bootstrap` was dropped: it pulled in the deprecated `popper.js@1` package to type the rest of the Bootstrap 4 surface, which these scripts never touch. |
| `plugins/toastr/toastr.min.js` | toastr 2.1.4 | `@types/toastr@2.1.44` |
| `plugins/summernote/summernote-bs4.js` | summernote 0.8.18 | `@types/summernote@0.8.10` (latest published) |
| `plugins/chart.js/Chart.min.js` | Chart.js 2.9.4 | `@types/chart.js@2.9.41` — the 2.9.x line targets Chart.js v2; `@types/chart.js@4` is for v3+. |
| `plugins/fullcalendar/main.js` | FullCalendar 5.5.1 | `@fullcalendar/core@5.5.1` (installed type-only; global `<script>` build is used at runtime) |
| `plugins/moment/moment.min.js` | moment 2.30.1 | `moment@2.30.1` (ships its own `.d.ts`; installed type-only). Bumped from 2.29.1 for CVE-2022-24785 / CVE-2022-31129; 2.30.1 is the last 2.x and a drop-in for global `<script>` use. |
| `js/mvc-grid/nonfactors-mvc-grid.js` | NonFactors.Grid.Mvc6 companion JS | not on npm → hand-written ambient in `global.d.ts` |
| `dist/js/adminlte.js` | AdminLTE 3.1.0 | no types — the custom scripts never call the AdminLTE JS API directly |

`global.d.ts` declares the globals with no matching `@types`: `MvcGrid`, `FullCalendar`,
and `moment`, plus a `JQuery.modal(...)` augmentation standing in for `@types/bootstrap`.

---

## How the migration was done (goal: zero behaviour change)

Every `site.ts` is the original `site.js` **wrapped in one IIFE**, with the smallest set
of type annotations needed to pass `tsc --strict`. Type syntax erases at compile time, so
the emitted `site.js` is byte-for-byte the original **modulo**:

* a leading `"use strict";` (from `alwaysStrict`),
* the `(function () { … })();` wrapper,
* **comments dropped** (`removeComments: true`) — write freely in the `.ts`; the shipped
  `.js` carries none,
* `tsc`'s own reformatting — it drops blank lines, splits `if (x) return;` onto two lines,
  parenthesises single arrow params `(e) =>`, strips the UTF-8 BOM, and adds ASI semicolons.

None of that changes semantics. At migration time every one of the 22 files was proved
equivalent by normalising the pre-migration original and the compiled output to a
whitespace-/semicolon-/comment-free token stream (minus the wrapper) and diffing — all matched.
Two deliberate follow-up changes have since landed (see "Deliberate post-port changes" below):
the `localizer.prevText` / `.nextText` casing fix, and removal of the dead `data.calendarEvent.calendarId`
line from the two Calendar `select` handlers — so those files' compiled `site.js` no longer
token-matches the pre-migration original, which is intentional. Going forward, `tsc` (types)
and `../TypeScripts.Tests` (behaviour) are the guards; a plain `git diff wwwroot/**/site.js`
shows exactly what a `.ts` edit changed in the output.

### Why an IIFE and no `Common/` module

All 22 scripts stay plain global `<script>` files, exactly as today (`moduleDetection: "legacy"`,
no `import`/`export` anywhere). Loading one `tsc` "program" over all 22 would otherwise collide
on the many same-named top-level helpers (`localizer`, `ApplyAllowedOptions`, `base64ToBlob`,
`ChangeCulture`, …). Wrapping each file's body in an IIFE isolates those scopes. This is safe
here because **no Razor view uses an inline `on*=` handler** and **no script references another
script's globals** — verified before starting. Shared helpers therefore stay duplicated
per file (just typed); there is deliberately no shared module.

### The recurring annotations

| Original pattern | Becomes | Reason |
|---|---|---|
| `function f(x) {` (DOM-interop param) | `function f(x: string \| HTMLSelectElement \| …)` / `x?: string` / `x: any` | `noImplicitAny` |
| `$(sel).val()` used as a string (`location.href =`, `input.value`, ajax `headers`, `parseInt`, `.split`) | `$(sel).val() as string` | `.val()` is `string \| number \| string[] \| undefined` |
| `$(this).attr('x')` assigned to a `string` / before `.includes()` | `$(this).attr('x')!` | `.attr()` is `string \| undefined`; the attribute is always present |
| `$.ajax({ data: null })` | `data: null as any` | jQuery's `AjaxSettings.data` excludes `null` |
| `toastr.error(msg)` where `msg?: string` | `toastr.error(msg!)` (inside the guard where it's set) | `toastr.error`'s message param is non-optional |
| `const localizer = { … 44 × $(sel).val() }` | `const localizer: any = { … }` | one cast instead of 44 errors (see the casing fix under "Deliberate post-port changes"). |
| `.datepicker({ … beforeShow/onSelect … })` instance call | `.datepicker({ … } as any)` + `onSelect: function (this: any)` | `@types/jqueryui` demands the callbacks return `DatepickerOptions` |
| `new FullCalendar.Calendar(document.getElementById('calendar'), {…})` | `… getElementById('calendar')!, {…} as any` + `select/eventClick: function (arg: any)` | null-guard + `CalendarOptions` friction on `themeSystem` etc. |
| `$.datepicker._clearDate(x)` | `($.datepicker as any)._clearDate(x)` | undocumented method |
| `let x;` (no initialiser) / `let arr = [];` reused | `let x: any;` / `let arr: any[] = [];` | `noImplicitAny` on evolving types |
| `htmlDoc.querySelectorAll('img[data-file]')` then `imgTag.src` | `.querySelectorAll<HTMLImageElement>('img[data-file]')` | `Element` has no `.src` |

### Deliberate post-port changes

Two fixes on top of the byte-faithful port. Both are DOM/widget glue, not business rules,
so they stay here and not in `Maroik.Core.*`. Every other `.ts` still compiles to its
pre-migration `site.js` modulo the formatting listed above.

1. **`localizer.prevText` / `.nextText` casing** — the `localizer` object defines
   `PrevText` / `NextText`, but `$.datepicker.setDefaults` read them with a lower-case
   first letter. Corrected to the matching casing in the 6 files that call `setDefaults`
   (`admin/Calendar/AdminIndex`, `user/Calendar/UserIndex`, `user/Notice/FixedIncome`,
   `user/Notice/FixedExpenditure`, `user/AccountBook/Income`, `user/AccountBook/Expenditure`).

2. **Dead `data.calendarEvent.calendarId` line removed** from the FullCalendar `select`
   handler in `admin/Calendar/AdminIndex` and `user/Calendar/UserIndex`. The line
   `$createCalendarEventMyCalendar.val(data.calendarEvent.calendarId)` was copy-pasted
   from the sibling `eventClick` (edit) handler, where `data` is the AJAX `success`
   parameter. In `select` that callback is named `response` (the endpoint,
   `CalendarController.GetCalendars`, returns `{ result: true, calendars }` — no
   `calendarEvent`), and no enclosing scope or global binds `data`, so the line threw
   `ReferenceError` and aborted the rest of the synchronous handler (the modal never
   opened on drag-select). Removed; a brand-new event has no calendar to pre-select and
   the `<select>` is freshly repopulated. The `eventClick` uses of `data.calendarEvent.*`
   are untouched — `data` is a real parameter there. With no free `data` left,
   `declare const data: any;` was dropped from `global.d.ts`.

---

## Commands

Run from `Maroik.Website/`:

```bash
npm ci            # once, after checkout
npm run build     # tsc -p tsconfig.json   → writes wwwroot/**/site.js
npm run watch     # same, --watch
npm run check     # tsc --noEmit  (type-check only, for CI)
npm test          # npm run build + vitest run   (see ../TypeScripts.Tests)
```

## Build integration

* **`dotnet build` / `dotnet run`** — `Maroik.Website.csproj` has a `CompileClientTypeScript`
  target (`BeforeTargets="BeforeBuild"`) that runs `npm run build`, **guarded** by
  `Exists('$(MSBuildProjectDirectory)/node_modules/typescript')`. So it compiles on dev
  machines that ran `npm ci`, and is a silent no-op everywhere else (a TS error there fails
  the .NET build — that's intended). Force it with `-p:BuildClientTypeScript=true`.
* **Docker** — `Dockerfile` has a dedicated `node:22-alpine` `tsbuild` stage
  (`npm ci && npm run build`) whose `wwwroot/` output is `COPY --from=tsbuild` over the
  SDK build stage's copy. The .NET SDK image has no Node, so the csproj target stays a no-op.
* **`dotnet publish` with no Node at all** — falls back to the committed `wwwroot/**/site.js`.

`wwwroot/**/site.js` is committed for exactly that last reason. `node_modules/`,
`*.tsbuildinfo` and `.vitest/` are git-ignored in the repo-root `.gitignore`.

### Project-file hygiene

`Maroik.Website.csproj` keeps the Node/TS toolchain out of the .NET project:
`DefaultItemExcludes` drops `node_modules\**` from every glob (so `dotnet publish`
doesn't drag ~75 packages' `package.json` files in via the Web SDK's `**\*.json`
Content rule, and Solution Explorer doesn't choke), `TypeScriptCompileBlocked=true`
stops VS's bundled TypeScript build from double-compiling `TypeScripts/**`, and
`package.json` / `tsconfig.json` / `TypeScripts/**` / `TypeScripts.Tests/**` are
`<Content Remove>`d — still visible in the tree (as `None`), just not shipped.
The compiled `wwwroot/**/site.js` publishes as normal `wwwroot` content.

---

## Tests

Live in **`../TypeScripts.Tests/`** with their own **`README.md`**. In short: a Vitest +
jsdom suite, one `*.test.ts` per `site.ts` at the mirrored path, running the *compiled*
`site.js`. `npm test` (from `Maroik.Website/`) builds then runs them; `dotnet build` /
Rider / VS run them too (incrementally, on by default — see `TypeScripts.Tests/README.md`).
