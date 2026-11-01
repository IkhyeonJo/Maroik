---
paths:
  - "**/Budgets/**"
  - "**/*Budget*"
  - "**/Formulas/**"
  - "**/*Formula*"
  - "**/AccountBook/**"
  - "**/*AccountBook*"
  - "**/*ExcelExport*"
---

# Intended design: Budgets (Income Budget / Expenditure Budget)

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- The two AccountBook sub-menu pages render one view (`Views/AccountBook/Budget.cshtml`) and one script, told the kind. A sheet is
  one (account, kind, year) and is handled like a spreadsheet: fixed rows per class (header, a line per sub-class and currency,
  subtotals, totals), 30 free rows, columns A..Z (B..M months, N year total, O..Z free). Which cells are editable is decided by the
  domain (`BudgetRow.EditableColumns`) and sent with the sheet; the script never derives it.
- Formulas are an Excel-compatible language implemented in `Maroik.Core.Domain/Formulas` (lexer, recursive-descent parser, binder,
  tree-walking evaluator, rewriter, highlighter, dependency graph with Tarjan / Kahn, `RecalculationEngine`). User text is never
  compiled to code: a closed set of nodes and the `FormulaFunctions` catalog. Values are decimals and are never stored — every
  request rebuilds and recalculates the account's workbook (all its sheets, the referenced ones, last year's) from the stored inputs
  and the year's actual records, so a value cannot be stale. The "reactive" recalculation is per request (an edit recalculates what
  it reaches, and the reply lists only the changed cells).
- Inputs are stored with stable row keys (`B{L.<main>.<sub>.<currency hex>}`), not row numbers, so adding or removing a line never
  breaks a reference and a formula copied from last year keeps working; a removed line's references show `#REF!` (and come back
  if the line is added again). A circular reference is `#CYCLE!` (Excel only warns).
- The formula bar's colouring is computed by the server on every pause in typing (`PreviewBudgetCell`); the client only paints
  it. The round trip is accepted; do not move the lexer / parser into `site.ts`.
- A budget cell's value is its budget only as a number from 0 to `FinanceAmountPolicy.MaxAbsoluteAmount`, rounded to 4 decimals;
  otherwise `#NUM!`. Actuals are the real income / expenditure records of the month in the owner's time zone, by currency label
  (no conversion); fixed schedules do not count until recorded.
- A sheet is stored on its first write (account row locked, default lines in the default monetary unit); every later write locks
  the sheet row (FOR UPDATE). The Excel download writes calculated values, not formulas (`ACTUAL` and the sheet names have no Excel
  equivalent). Undo / redo lasts for the page session only.
