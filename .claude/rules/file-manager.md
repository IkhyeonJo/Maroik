---
paths:
  - "**/*FileManager*"
  - "**/FileManagers/**"
  - "**/FileManager/**"
  - "Maroik.FileStorage/**"
  - "Maroik.FileStorage.Tests/**"
  - "**/*Clamav*"
  - "**/*FileClient*"
  - "**/*Upload*"
  - "Maroik.Worker/**"
  - "Maroik.Worker.Tests/**"
  - "**/docker-compose*"
  - "**/*Management*"
---

# Intended design: File Manager

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- One drive per user (`/FileManager/UserIndex`, User role only), a folder tree of `FileManagerNode` rows. The default quota is
  0 GB (`Account.FileManagerQuotaBytes` = 0): nobody can upload until an administrator sets a quota in `Management/Account`.
  The demo account is treated like any other account.
- Every structural write (create folder, rename, move, delete, begin / complete upload) first locks the owner's `Account` row
  (`FindByEmailForUpdateAsync`) inside its transaction, so concurrent writes of one user cannot overrun the quota or create twin
  names. FileStorage calls (abort, delete of the replaced or deleted files) run after the commit; a failed delete leaves an orphan
  file, which is logged, not retried.
- `Maroik.FileStorage`'s `api/FileManager/delete` refuses any path that is not `upload/FileManager/{guid}`. The only other delete API is
  `api/ClassModel/delete` (class-suggestion model files, `upload/ClassModels/{guid}.zip` only; see Finance). Board / note / calendar
  attachments still have none (see Files and content).
- Uploads go in 16 MB chunks to a staging file and are scanned by ClamAV on completion. There is no file size limit (since
  2026-10-25, owner's request; only the quota bounds it), but ClamAV scans at most 2,147,483,645 bytes (clamd clamps its stream limit
  to that; both compose files set `CLAMD_CONF_*` to 2048M and `MaxScanTime` to 10 minutes) — `FileManagerUploadPolicy.MaxScannableBytes`
  on the domain's side, `ClamavClient.MaxStreamBytes` on the scanner's. A larger file can only be stored without a scan: `BeginUpload`
  refuses it ("larger than 2 GB") unless the locked account row has the scan turned off, and `ClamavClient` never sends a stream that
  long (it answers `LimitExceeded` at once). The quota check compares in decimal (since 2026-11-02): the long sum of the usage and a
  huge declared size overflowed into "fits" once the usage was over the quota, and a scan-off account has no size limit. A scan that hits a clamd limit is never clean: it is `ClamavScanResult.LimitExceeded`
  (`FileStorageRefusals.ScanLimitExceeded`), kept apart from a scanner outage (`Unavailable`) in the logs; both are refused as "could
  not be scanned" and the staging file is dropped.
- Abandoned uploads (since 2026-10-26, owner's request; it replaced "24 hours after the start, at the user's next upload"): an
  unfinished upload is abandoned after `FileManagerUploadPolicy.StaleUploadAge` (1 hour) without activity, counted from its last
  activity, not its start — `Updated` of an `Uploading` row is stamped by every stored chunk and every scan poll that answers
  "scanning" (`RecordUploadActivityAsync`, a single column write, no transaction). An upload that keeps sending is never removed,
  however many days it takes (a 16 MB chunk within the hour needs ~4.7 KB/s); an hour of silence means the page is gone, and the page
  cannot resume a lost upload anyway. The worker's `FileManagerUploadSweepScheduler` runs `FileManagerService.RemoveStaleUploadsAsync`
  at start-up and every hour (at most 100 accounts per run, each in its own transaction: account row locked, idleness judged again
  under the lock, rows deleted, abort sent after the commit). `BeginUpload` still removes the account's idle uploads too, and
  `CheckQuota` (the check before a batch) does not count them, so an account they filled can always start the upload that removes
  them. The worker reads `FileStorage:BaseUrl` like the website. A `complete` that fails (timeout, lost answer — not a refusal) is
  aborted at once (since 2026-11-02, owner's request); when that abort fails too the row stays `Uploading` for this sweep, since a
  dropped row would leave the staging / stored file where no sweep finds it.
- "Turn off the virus scan for files" (since 2026-10-25, owner's request; it replaced the "keep a file the scan could not finish"
  question flow): an administrator ticks it per account in `Management/Account` (`Account.FileManagerScanDisabled`, default false,
  in the grid, its search, the Excel export and the admin update audit line). `CompleteUpload` reads it when the upload completes and
  sends `skipScan` with `api/FileManager/complete`; FileStorage then moves the whole staging file into place at once (200, no scan
  queued; a scan already queued is still followed) and logs it at Warning, as does `FileManagerService`. Such a file carries no mark:
  it is listed, previewed and downloaded like any other, and turning the scan back on leaves it as it is. With the scan on, nothing is
  asked: a file the scan could not finish is refused. Only the File Manager is affected — attachments (posts, notes, events) are
  always scanned and refused whenever the scan is not clean.
- The virus scan runs in the background (since 2026-10-25, owner's request: a 484 MB zip took 202 s, past the 100 s of the
  HttpClient and of Cloudflare). `api/FileManager/complete` queues the scan (`IFileManagerScanQueue`, in memory) and answers 202 until
  `FileManagerScanWorker` (2 scans at once) has stored the file (200) or refused it (400, answered once); the page asks
  `CompleteUpload` again every `FileManagerUploadPolicy.ScanPollMilliseconds` and gives up (abort) after `MaxScanWaitMilliseconds`
  or on Cancel, and sends the next file meanwhile. Completing is idempotent (an already completed upload answers "stored"). A
  restart forgets the queue: the next `complete` of a whole staging file queues it again. A page closed during the scan leaves the
  row `Uploading`, and the hourly sweep's abort removes the stored file too (abort drops staging, stored file and pending scan).
- An overwrite keeps the old row until the new upload completes: the unique name index is partial (`WHERE NOT "Uploading"`), so
  the uploading twin may coexist with it.
- Downloading several items downloads each file separately (no ZIP). The page fetches each file whole before the browser saves it,
  in a download dialog whose bar is measured against the file's size in bytes the server sends with the item / `ListDownloadFiles`
  (the streamed response carries no Content-Length); Cancel aborts the file being fetched and skips the rest. Each role's
  `ajaxError` handler ignores a request aborted on purpose (`thrownError === "abort"`); every other failure still redirects.
- Preview is images only (png / jpg / gif / webp, proven by their header bytes) up to 100 MB.
