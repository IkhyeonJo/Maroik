# Security policy

Maroik is maintained by one person and runs live at [www.maroik.com](https://www.maroik.com). Reports are welcome and
are handled on a best-effort basis; there is no bug bounty.

## Supported versions

Only the latest commit on `main` is supported — it is what the live site runs. Fixes are not back-ported.

## Reporting a vulnerability

**Do not open a public issue or pull request for a vulnerability.** E-mail it to `admin@maroik.com` with "Security" in
the subject, and include:

- the affected page, endpoint or file (and commit, if you tested a local build),
- the steps to reproduce it and what an attacker gains,
- any proof-of-concept request, payload or screenshot.

You will get an answer once the report has been read.

## Testing against the live site

- Use the public demo account from the README (`demo@maroik.com`) or an account you registered yourself.
- Do not read, change or delete data that belongs to another account. Stop and report as soon as you can show access.
- No denial-of-service, load or volume testing, and no automated scanners that flood the site or the mail relay.
- No social engineering, phishing or physical attacks.

Prefer a local build for anything intrusive: the README describes how to run the whole stack with Docker.

## Out of scope

These are public or deliberate, so they are not vulnerabilities:

- The demo account credentials published in the README, and the debug-only values in `.env.debug` (generated for this
  public repository; production uses its own secrets).
- Services that are reachable only on the internal Docker network (for example `Maroik.FileStorage`) carrying no
  app-level authentication: the network boundary is their security boundary. A way to reach one from outside **is** in
  scope.
- The other behaviours recorded as intended design in `CLAUDE.md` and `.claude/rules/`, unless you show a concrete
  exploit the rationale there does not account for.
- Missing hardening with no demonstrated impact (version banners, best-practice headers alone, rate limits on read-only
  pages).
