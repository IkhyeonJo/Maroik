---
paths:
  - "Maroik.Website/Controllers/**"
  - "Maroik.Website.Tests/Controllers/**"
  - "Maroik.Website/Resources/Controllers/**"
  - "**/*Result*"
  - "**/ServiceErrorKeys*"
---

# Intended design: Error handling

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- Controller actions keep their per-action `catch` blocks and return HTTP 200 with
  `{ result, error }`; exceptions are logged on the server. A genuine input-validation failure shows its
  specific reason (or "Input is invalid"), while an infrastructure failure shows a generic
  "temporary error, please try again" message.
- Every `[HttpPost]` action is accounted for in `ControllerFailureContractTests`: either a row that makes its service throw
  and asserts the generic failure (HTTP 200, no exception text), or an `_exemptPosts` entry with the reason (non-AJAX
  form posts and plain file downloads, which fall to the global exception page). `EveryPostAction_IsCoveredByAFailureContractTest_OrExemptWithAReason`
  fails for a new POST action until it has one of the two.
