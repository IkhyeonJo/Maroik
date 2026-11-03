---
paths:
  - "**/Finances/**"
  - "**/AccountBook/**"
  - "**/*AccountBook*"
  - "**/Notice/**"
  - "**/*Notice*"
  - "**/*Income*"
  - "**/*Expenditure*"
  - "**/*Asset*"
  - "**/*Amount*"
  - "**/*FixedSchedule*"
  - "**/*Dashboard*"
  - "**/Dashboard/**"
  - "**/*ExcelExport*"
  - "**/*Finance*"
---

# Intended design: Finance

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- An asset's currency label can be edited at any time (e.g. "$" → "USD"). Never reject a currency
  change, whatever the existing balance looks like.
- A transfer-type expenditure (deposit / investment / public pension / debt repayment), one-off or fixed,
  requires its payment method and its deposit asset to carry the same currency label, on create and on update. Relabelling only one
  of the two assets therefore makes editing an older transfer fail — intended.
- A negative amount typed into an income / expenditure (one-off or fixed) is stored as its absolute value
  (`Math.Abs`).
- The dashboard's first year (`StartYear`) is found from the account's earliest income / expenditure by loading
  them all.
- `FixedSchedulePolicy.IsNoticed`'s behavior around the year end and 29 February is accepted as it is.
- Amounts are shown with every decimal place the `numeric(20,4)` columns store: the finance grids use
  `AmountDisplay.GridFormat` (`#,0.####`, trailing zeros dropped) and the dashboard tooltips read
  `FinanceAmountPolicy.MaxDecimalPlaces` from `#amountMaxDecimalPlaces`. The currency is a free-text label, so there is
  no per-currency number of decimals (USD 12.50 shows as "12.5").
- The edit forms receive amounts as JSON numbers, so an amount beyond ~15 significant digits (≥ ~9×10¹⁵) can
  change on a re-save. Accepted; not a defect to raise.
