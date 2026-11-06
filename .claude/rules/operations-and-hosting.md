---
paths:
  - "**/docker-compose*"
  - "**/Dockerfile"
  - "**/*.dcproj"
  - "Maroik.DeployOps/**"
  - "Maroik.Crontab/**"
  - "Maroik.Caddy/**"
  - "Maroik.SSL/**"
  - ".github/**"
  - "**/Program.cs"
  - "**/appsettings*.json"
  - "**/*Hosting*"
  - "**/WorkerHost*"
  - "**/*ForwardedProto*"
  - ".env*"
  - "**/*DataProtection*"
  - "**/*HealthCheck*"
---

# Intended design: Operations and hosting

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- CI/CD (`.github/workflows/ci-cd.yml`) runs on pushes to `main`, pull requests into `main` and on demand only — not on
  pushes to other branches. A feature branch is checked by opening its pull request (or a manual run). Do not widen the
  triggers.
- Maroik only ever runs under docker-compose on Linux (Ubuntu). Behavior that differs only on a Windows dev box
  (path separators, a test that fails only there) is an environment difference, not a defect.
- The containers run as root (no `USER` in the Dockerfiles).
- In production TLS ends at Cloudflare: the site is reached only through a Cloudflare Tunnel, which hands requests
  to the website container over plain HTTP, so there is no origin certificate. `Program.cs` takes
  `X-Forwarded-Proto` from private-network peers only (pinned by `ForwardedProtoTests`).
- `Deploy.sh` choice 1 (build here) also asks how the site is reached and lays `docker-compose.prod.<mode>.yml` over the
  stack; the website reads it as `ServerSetting:HostingMode` (`HostingProfile`; unset = `Tunnel`, which choice 2 and
  `AutoDeploy.sh` always run). `PortForwarding`: Caddy (`Maroik.Caddy/Caddyfile`) ends HTTPS with a Let's Encrypt
  certificate on 80 / 443 and is trusted like cloudflared. `NoDomain`: plain HTTP on a published port for a home network —
  no HSTS / https / www redirects, cookies without Secure and without the `__Secure-` prefix, no forwarded scheme taken
  (pinned by `HostingModeStartupTests`); the culture cookie too sets no Secure flag of its own (since 2026-11-02,
  `DashboardControllerCultureCookieTests`) and takes the cookie policy's. Passwords travel unencrypted there; that is the owner's accepted trade-off.
  The answers are asked on every deploy, never saved.
- `AllowedHosts` is `*`. The `amqp://guest:guest@localhost` fallbacks (`Program.cs`, `WorkerHost`) are never
  used in a deployment (the env files always set the connection string).
- Data Protection keys are stored unencrypted (Valkey, or the file fallback); they only live on the server.
- Every response carries `Cache-Control: no-cache, no-store`, static files (JS / CSS / images) included: the header
  middleware in `Maroik.Website/Program.cs` runs before `UseStaticFiles`, and nothing overrides it. This is on purpose, so
  the login page (and the pages after a logout) always come up fresh. Do not add browser caching for static files
  (`OnPrepareResponse` `max-age` / `immutable`), even though the `asp-append-version` fingerprints would allow it.
- `Backup.sh` runs once a day at 04:00 server time (`Maroik.Crontab/InitCron`; since 2026-11-02, owner's decision — it ran hourly
  before). A run copies and xz-compresses the whole upload tree, File Manager included, and can outlast an hour on a large drive;
  the script has no lock. A lost server loses up to a day of data; that is the accepted trade-off.
