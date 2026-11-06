---
paths:
  - "**/*Account*"
  - "**/Accounts/**"
  - "**/Account/**"
  - "**/*Password*"
  - "**/*Nickname*"
  - "**/*Login*"
  - "**/*Register*"
  - "**/*GuidToken*"
  - "**/*SignIn*"
  - "**/Profile*"
  - "**/*ProfileService*"
  - "**/*Session*"
  - "**/*Management*"
---

# Intended design: Accounts

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- A failed-login lockout is permanent until the user resets the password or an admin unlocks the
  account. It only refuses new logins: sessions already signed in keep working, so guessing wrong on purpose
  cannot throw the owner out. An administrator locking an unlocked account (`Account.LockAndEndSessions`)
  also replaces the security stamp and so ends the account's sessions. A successful password reset always unlocks the account, including an admin-imposed lock, and signs
  the user in straight away (fresh session id, new security stamp) unless the account has not accepted the
  service terms. This does not weaken the lockout: the reset needs the single-use mailed token, not a guessed
  password. Admin sanctions use `AdminResetPassword` + `MustChangePassword`, not `Locked`.
- Registration and password-reset tokens are stored in the database only as their SHA-256 hash (`GuidToken.Hash`,
  since 2026-10-30, owner's request; the columns keep their names): only the mailed link carries the token, so a leaked row
  cannot be turned into a working link. The account is looked up by the hash, the 24-hour expiry is read from the token the
  link presents (the hash binds its timestamp), and a resend always mints a new token, so only the latest confirmation
  mail's link works (pinned by `TheStoredHash_PresentedAsTheToken_IsRefused`,
  `Register_StoresOnlyTheHashOfTheMailedToken_AndTheMailedLinkStillWorks`). A fast unsalted hash is enough: the token
  holds 16 CSPRNG bytes. `HashedPassword`, `RegistrationToken` and `ResetPasswordToken` never reach the admin screens:
  `IManagementAccountService` returns plain `AccountResponse` rows, so the account grid, its whole-row search
  and the Excel export neither show nor match them (a live token on screen hands the account to whoever sees it;
  a searchable token or hash is a guessing oracle). Pinned by `AccountGrid_DoesNotRenderTheHashOrTheTokens`,
  `SearchAsync_DoesNotMatchTheHashOrTheTokens` and the `ManagementAccountServiceTests` row checks. Do not add
  them back.
- A successful self-service password change ends the session; the user signs in again (as on other sites), and
  every other session of the account is invalidated through the security stamp. Because it proves the current
  password, it also lifts a lock and clears the failed-login counter (`Account.ChangePassword`) — otherwise an owner
  locked by someone else's guesses, still signed in, would change the password and then be refused at that sign-in.
- A deleted account's login answers "Your Account is Deleted. Please contact the administrator." — it is not
  re-registrable.
- The Login page is pre-filled with the public demo account (`demo@maroik.com` / `demoO12!!`), and the demo
  account's dashboard always shows the fixed period from `ServerSetting.DemoDashboardYear/Month`. Its calendar first
  opens on that same period (`ICalendarService.GetFixedInitialDate`) and its budget pages on that year
  (`IBudgetService.GetFixedInitialYear`); the toolbars can still page away from them.
- Nicknames reject `<`, `>`, `"`, the backtick and `\` (after NFKC) but allow `&` and `'` ("R&D team",
  "O'Brien"): nicknames are always output encoded, so those two are not an injection vector. An
  administrator may create an account under a reserved nickname; self-registration may not.
- IP-based rate limiting is done at Cloudflare. Do not add `AddRateLimiter` or IP-keyed throttling in
  the app (behind Cloudflare, `RemoteIpAddress` is an edge address shared by every visitor). Login BCrypt CPU
  cost and registration / reset mail volume are infrastructure concerns in the same way.
- The public demo account has no server-side write protection: anyone can change its password, lock it or post
  as it. Anyone can also lock any account with `MaxLoginAttempt` wrong guesses. Both are accepted.
