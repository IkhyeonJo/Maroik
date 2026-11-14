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
  - "**/Classifiers/**"
  - "**/*ClassSuggestion*"
  - "**/*Outlier*"
  - "**/*Forecast*"
  - "**/*Insight*"
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
- Amounts are shown in their currency's usual decimal places when the currency label is a known ISO 4217 code — none for KRW
  or JPY, two for USD or EUR ("12.50"), three for KWD — and with every decimal place the `numeric(20,4)` columns store (trailing
  zeros dropped, `#,0.####`) when it is not, since the label is free text ("$", "원"). Owner's decision, 2026-11-14 (before, every
  currency showed every stored place). `AmountDisplay.Text` decides it for the finance grids (`AmountText`, sorted and filtered
  on the numeric `Amount`), the dashboard sentences, and the dashboard tooltips (`#amountMinDecimalPlaces` /
  `#amountMaxDecimalPlaces`). Display only: what is stored, validated and calculated is unchanged; a rounded figure is rounded
  half away from zero. Not covered: the Excel exports (cell values stay exact) and the budget sheets (spreadsheet cells).
- The edit forms receive amounts as JSON numbers, so an amount beyond ~15 significant digits (≥ ~9×10¹⁵) can
  change on a re-save. Accepted; not a defect to raise.

## Analytics and suggestions (since 2026-11-14)

Added with the owner's development guidelines (evidence before technique: each choice below was measured on the owner's 8,649
expenditures and 581 incomes against simpler alternatives). They only read the ledger; none of them writes or changes a record.

- **Class suggestion** (income and expenditure create forms): a hint under the content field, applied only when the user clicks it.
  The Domain decides what may be suggested and from what (`ClassSuggestionPolicy`: content needs a letter or digit; learn only from
  records of a valid taxonomy pair); `ContentClassifier` answers everything else (ML.NET: TF-IDF character 1–3-grams + maximum-entropy
  logistic regression, seed 0, one thread). Measured with monthly retraining on the newest 1,848 expenditures / 125 incomes: 98.48 % /
  98.40 % correct. Alternatives measured and not taken: a "same content as before" rule in front of the model (never right where the
  model was wrong; removed 2026-11-14), "the most recent class for that content" (98.00 %, worse at p = 0.004), character 1–4-grams or
  added word features (no gain), a character-bigram naive Bayes (91.3 % on a static split).
  ML.NET lives in `Maroik.Core.Service` (an in-process library like Magick.NET — not `Core.Client`, which is for external systems;
  Domain and Contract take no package but `ErrorOr`). `ContentClassifierCache` keeps one model per (book, account) in memory,
  retrains when the learnable records' content or classes change (a fingerprint), and logs every training with
  `ContentClassifier.ModelVersion`; nothing is persisted, so a restart retrains on first use.
- **Unusually large expenditures** (the grid's "Amount check" column): the box-plot fence Q3 + 1.5 × IQR on ln(1 + amount), per
  sub-class and currency label, groups of at least 8. The logarithm (a ratio, not a difference) flags 1.9 % of the owner's records
  instead of 4.8 % on raw amounts. It is computed in `double`: it only decides a flag and produces no amount.
- **Month-end projection** (dashboard, current month only): spent so far + the average of what the last 3 months with a record
  spent after today's day. On 59 months of all expenditures it beat a 12-month average, a same-month-last-year and a Holt-Winters
  model (MASE 0.44–0.54 against a naive "same as last month"; on consumer spending alone a 12-month average was slightly better,
  0.42–0.53), so no time-series model is used. It is shown with its date and, from 3 earlier months on,
  the mean absolute error of the same projection for those months. No prediction interval is shown: empirical 80 % intervals from
  past errors covered only 55–79 % of later months.
- **Spending facts** (dashboard): weekend against weekday daily average, the largest sub-class and its share, the change from the
  month before (the same days for the current month) — descriptive statistics only, no significance tests.
- The projection and the facts read the expenditures of the selected month's `ExpenditureForecastPolicy.HistoryMonths` (12) months
  before as well (owner's decision, 2026-11-14), so January is compared with December and the projection and its typical error
  work the same all year. It is still one expenditure query per dashboard (`GetSummaryAsync_DerivesYearMonthTotals_FromSingleYearRangeQueryPerRepository`),
  now from the history start to the year's end, and the year's totals are cut from it (`GetSummaryAsync_TheMonthsBeforeTheYear_AreNotInTheYearsTotals`).
  Measured on 8,645 records over 5½ years: the widest range (January, two years) took about 1.2 ms, one year 0.35 ms.
