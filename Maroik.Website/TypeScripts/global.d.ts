/*
 * Ambient declarations for globals loaded via <script> in Views/Shared/_Layout.cshtml
 * (and Views/Account/_Layout.cshtml) that have no matching @types package.
 *
 * This file has NO top-level import/export, so it stays a global script and its
 * declarations apply across every TypeScripts/**\/*.ts without an import.
 * (import("x") in a *type* position does not turn the file into a module.)
 */

// NonFactors MVC.Grid 6 client companion — wwwroot/{area}/js/mvc-grid/nonfactors-mvc-grid.js.
// Not published to npm (NuGet-ecosystem vendored JS). Typed loosely to the surface the
// custom scripts actually touch: new MvcGrid(el), .url.searchParams, .reload().
declare class MvcGrid {
    constructor(container: Element | null);
    url: URL;
    reload(): void;
    [key: string]: unknown;
}

// FullCalendar 5.5.1 global build — wwwroot/{area}/plugins/fullcalendar/main.js.
// Backed (type-only) by the @fullcalendar/core@5.5.1 dev dependency.
declare const FullCalendar: typeof import("@fullcalendar/core");

// moment 2.29.1 global build — wwwroot/{area}/plugins/moment/moment.min.js.
// Backed (type-only) by the moment@2.29.1 dev dependency (ships its own d.ts).
declare const moment: typeof import("moment");
