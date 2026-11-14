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
  - "**/*ClassModel*"
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
  on the numeric `Amount`) and the dashboard tooltips (`#amountMinDecimalPlaces` /
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
  Domain and Contract take no package but `ErrorOr`).
- **Where the model is trained and kept** (since 2026-11-14, owner's request): the worker trains, the website only reads.
  - A suggestion (`ClassModelSource`) reads the account's `ClassModel` record (one per account and book: file path, the
    fingerprint of the records it learned, `ContentClassifier.ModelVersion`, time) and the model file it names, and keeps the model
    in `ClassModelCache` while it is used (owner's decision, 2026-11-14): one unused for the session time
    (`ServerSetting.SessionExpireMinutes`, the session's idle timeout) is dropped by the sweep that runs every minute — the session's
    end itself cannot be watched (a closed browser tells the server nothing; the sessions live in Valkey). Whatever the number of
    users, at most `DefaultCapacity` = 50 accounts' books are kept (least recently used out; a model of 8,649 records is a 655 KB
    file and ~6.5 MB in memory). A model of older records still answers; none of another version is read.
  - No model, an older one or an unreadable one: it publishes `TrainClassModelMessage` on `maroik.class-model-training`, once per
    fingerprint within `ClassModelCache.TrainingRequestRetryAfter` (10 minutes). So an account's very first question (and the first
    after a redeploy that recreated the database) answers nothing until the worker is done (about a second). A restart only reads the
    stored file again.
  - The worker (`ClassModelTrainingWorker`, prefetch 1, one training at a time) runs `ClassModelTrainingService`: it saves the model
    as a new file `upload/ClassModels/{guid}.zip` (`api/ClassModel/save`: no virus scan and no size limit — the worker wrote it, no
    user did; the storage never overwrites), points the record at it, and only then deletes the replaced file
    (`api/ClassModel/delete`, which accepts only such a path). A record write that fails deletes the new file again; a failed delete
    of an old file leaves an orphan, logged. A failed training is logged and acked: no retry or dead-letter queue, the next suggestion
    asks again.
- **Unusually large expenditures** (the grid's "Amount check" column): the box-plot fence Q3 + 1.5 × IQR on ln(1 + amount), per
  sub-class and currency label, groups of at least 8. The logarithm (a ratio, not a difference) flags 1.9 % of the owner's records
  instead of 4.8 % on raw amounts. It is computed in `double`: it only decides a flag and produces no amount.
- The dashboard has no month-end projection or spending-facts card: added on 2026-11-14 and removed the same day at the owner's
  request.
