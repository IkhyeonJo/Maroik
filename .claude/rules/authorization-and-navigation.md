---
paths:
  - "**/*Authorization*"
  - "**/Filters/**"
  - "**/*Menu*"
  - "**/Menus/**"
  - "**/*Management*"
  - "**/Management/**"
  - "**/*Admin*"
  - "**/*Dashboard*"
  - "**/Dashboard/**"
  - "**/_Layout*"
  - "**/Layouts/**"
  - "**/*Culture*"
---

# Intended design: Authorization and navigation

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- The navigation menu is the access-control list: a `Category` maps to one controller, a `SubCategory`
  to one action, and `AuthorizationFilter` allows a GET only when it resolves to a menu item for the
  role. Controller boundaries follow the menu, so large controllers (organized with `#region`) and
  mode-switching GET actions such as `FreeForum(method = "edit")` are intended. Do not split controllers
  (including into partial classes) or add GET actions that would need new menu rows.
- `AuthorizationFilter.Deny()` redirects to `/Dashboard/AnonymousIndex`, and each role's `_Layout`
  script has a global `ajaxError` handler that sends every failed AJAX call to the same page. The two
  work as a pair; keep both.
- Admin safeguards (`AdminSafeguardPolicy`, applied by `ManagementAccountService` update and delete): an administrator
  cannot lock, delete or demote their own account (re-saving it unchanged, unlocking and restoring it stay allowed), and
  nobody can lock, delete or demote the last active administrator (Admin, not locked, not deleted). Each admin edit / delete
  first locks the active administrators' rows (`FindActiveAdminsForUpdateAsync`, email order), then the edited row, so two
  concurrent edits cannot leave no administrator. A failed-login lockout is not covered (see Accounts).
- Menu safeguard (`MenuSafeguardPolicy`, applied by `MenuService` category / sub-category update and delete): a change that
  takes away the administrators' last route to `Management/Menu` — the Admin "Management" category and its Admin "Menu"
  item in the seed — is refused, or nobody could repair the menu from the UI. Every other entry stays the administrator's
  call, and so does that one while a second route to it exists; a menu that already had no route is not held to one. Each
  update / delete first locks every category, then every sub-category (`GetAllForUpdateAsync`, id order), then the edited
  row, so two concurrent menu writes can neither both pass the check nor deadlock. `MenuSnapshot` judges a menu with
  `MenuNavigationMatcher`'s rule (pinned by `MenuSafeguardTests.TheSnapshot_AgreesWithTheNavigationMatcher`).
- The admin-only GET pages (`Management/Account`, `Management/Menu`, `Dashboard/AdminIndex`, ...) are protected
  by the menu alone, while their POSTs also carry a role attribute. Getting the menu's `Role` right is the
  administrator's job; do not add a second role check to those GETs.
- `AuthorizationFilter` sends a signed-out visitor who opens an unlisted page to the public dashboard without
  logging it (pinned by `OnAuthorizationAsync_LogsNothing_WhenAnAnonymousVisitorIsSentFromAnUnlistedPage`).
  Denials of signed-in accounts go through `Deny()` and are logged.
- The `ReturnUri` the layout uses after a culture change is not an open redirect: the router answers 404 to any
  `//…` or `/\…` path, so no layout renders it. No extra validation is wanted.
