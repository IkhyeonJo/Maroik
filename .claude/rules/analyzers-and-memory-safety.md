---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
  - ".editorconfig"
  - "**/*.globalconfig"
  - "global.json"
  - "BannedSymbols.txt"
  - "stylecop.json"
  - "**/Dockerfile"
  - ".github/workflows/**"
---

# Memory safety — resource-lifetime analyzers are build errors

`.editorconfig` (`[*.cs]`, "Memory safety" block) sets these rules to `error`, for production and test code alike:
CA2000, CA1001, CA2213, CA2215, CA2216, CA1063, CA1816, CA2025 (disposal / ownership), CA2014, CA2012, CA2015, CA2018,
CA1821, CA2011 (stack, buffers, ValueTask, finalizers). Only these rules are turned on, not `AnalysisLevel=latest-all`
(that wider attempt was rolled back on 2026-10-15). No project allows `unsafe` code.

Further rule-ID blocks in the same file, also `error`: "Logging templates" (CA2254, CA2017, CA1727 — see Logging) and
"Async and cancellation" (CA2016: forward the `CancellationToken` you received; CA1849: call the async API in an async method,
e.g. `await app.RunAsync()`, `await Log.CloseAndFlushAsync()`, `SaveChangesAsync`, `CancelAsync`).

Also `error`, by rule ID:
- **Security** ("Security" block): .NET's injection-taint, XML, antiforgery (CA3147), deserialization (CA23xx) and
  crypto / TLS / randomness (CA53xx) rules. CA3003 (file path injection) is left off on purpose: it offers no hook to declare
  `StoragePath.ResolveWithinRoot` as a sanitizer, so it could only be met with suppressions; path traversal is pinned by
  `StoragePathTests` and the `FileController` tests, and held in two ways by `FileSystemAccessArchitectureTests`. In
  `Maroik.FileStorage` (the one service whose paths come from a request) by the type system: `StoragePath` is a value only
  `StoragePath.ResolveWithinRoot` / `InDirectory` can make, `Helpers/StorageFiles` is the service's only code that names `File`,
  `Directory`, `FileStream`, `FileInfo` or `DirectoryInfo`, and its members take a `StoragePath`, never a `string` — an unchecked
  path does not compile into a file access. Everywhere else (paths from server configuration or a fixed folder) each use is
  listed by file, member and API with the reason its path is safe, and the list must match the code exactly: a new use, even
  inside a listed member, fails until it is listed after checking where its path comes from. SecurityCodeScan (whose SCS0018 did accept that declaration) was removed
  on 2026-10-20 — unmaintained since 2022 and the analyzer most likely to stop loading after an SDK bump. CA2100 is off for
  `*.Tests/**` only — the test fixtures create and drop throwaway databases with DDL, whose identifiers cannot be parameters.
- **String comparison and culture** (CA1304, CA1305, CA1307, CA1309, CA1310, CA1311): `StringComparison.Ordinal` for
  identifiers, paths and test assertions (xUnit's `Assert.Contains(string, string)` included), `CultureInfo.InvariantCulture`
  for machine formats (parsing, storage keys, log rendering, Excel cells, the domain's English messages),
  `CultureInfo.CurrentCulture` only for text shown to the user (`Int64Extensions` size labels). Inside an EF Core query a
  .NET `ToLower` runs as SQL anyway, so a case-insensitive match uses `EF.Functions.ILike` with `EscapeLike` /
  `LikeEscapeCharacter` (`AccountRepository.NicknameExistsIgnoreCaseAsync`), never `ToLowerInvariant` (not translatable).
- **Banned APIs** (RS0030, `BannedSymbols.txt`, Microsoft.CodeAnalysis.BannedApiAnalyzers): every production project,
  Domain and Contract included, may not read the clock directly (`DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow` —
  use the injected `TimeProvider`), block on async work (`Task.Wait/WaitAll/WaitAny`, `.Result`, `GetAwaiter().GetResult()`,
  `Thread.Sleep`) or write to `Console`. It is wired in `Directory.Build.props` for every `.csproj` whose name does not end
  in `.Tests` (a build-time analyzer, `PrivateAssets=all`, so not a runtime dependency — the `.csproj` package graph that
  `SolutionLayeringArchitectureTests` checks is unchanged). A method ID names one overload, so the file lists every overload;
  before bumping the analyzer, check that the `Microsoft.CodeAnalysis` version its DLL references (not the package number;
  5.6.0 references 4.12) is not newer than the SDK's compiler (Roslyn 5.0), or the analyzer silently fails to load (CS8032).

- **Unnecessary usings** (IDE0005): it runs at build time only because `Directory.Build.props` sets
  `GenerateDocumentationFile` (which also checks doc-comment references such as `paramref`; missing doc comments, CS1591,
  stay allowed). In a C# 14 `extension(T value)` block the receiver is not a `paramref` target — say "this value" instead.

`UnitOfWork.RollbackAsync` / `DisposeAsync` ignore only a `DbException` (the database already ended the transaction);
any other rollback failure propagates after the transaction is released and the tracker cleared (pinned by
`UnitOfWorkTests.*_Rethrows_AFailureThatIsNotADatabaseError_AndStillClearsTheTracker`).

The code has no `#pragma warning disable`.

- **One pinned SDK.** `global.json` pins the exact .NET SDK (`rollForward: disable`); CI installs it from that file and every
  Dockerfile's build stage uses the same `sdk:<version>` tag (pinned by `SdkPinArchitectureTests`). The analyzers are build
  errors, so an SDK bump (a newer Roslyn) is one deliberate change to all of these at once. The runtime images (`aspnet` /
  `runtime`) stay on the floating `10.0` tag so they pick up runtime security patches. `.github/workflows/sdk-bump.yml` checks
  weekly for a newer SDK of the pinned channel and opens one pull request that moves all of them together (Dependabot would
  split global.json and the Dockerfiles into pull requests that each fail the test); it starts CI/CD on that branch itself,
  since a pull request opened with `GITHUB_TOKEN` triggers no workflow. It needs the repository setting "Allow GitHub Actions
  to create and approve pull requests".

- **CA2000 stays on the default `dispose_analysis_kind`, not `AllPaths`** (tried 2026-10-16). In `AllPaths` mode it reports
  the construction site of a type whose async `DisposeAsync` awaits e.g. `Task.WhenAll` / `Task.Yield`, even after every owned
  field was disposed (`FakeSmtpServer`), so it could only be met with suppressions. The genuine exception-path leaks that run
  surfaced were fixed: a constructor / factory that starts a listener closes it if starting fails (`FakeSmtpServer`,
  `FakeClamavServer.Start`), a semaphore is created right before the try that disposes it (`HtmlParserService`), and
  `RabbitMqEmailPublisher.DisposeAsync` is idempotent (a second call returns at once; a call that timed out disposed nothing
  and can be repeated).
- **Configure analyzers by rule ID only.** `AnalysisLevel=latest-all` (or any `AnalysisMode`) ships an SDK globalconfig that
  sets every rule by ID, and a rule-ID entry beats a category or all-analyzers entry whichever file it is in (`.editorconfig`
  wins over the globalconfig only at the same level). So under `latest-all` a `dotnet_analyzer_diagnostic.severity = ...` or
  `dotnet_analyzer_diagnostic.category-<X>.severity = ...` line in `.editorconfig` is silently ignored, while
  `dotnet_diagnostic.<ID>.severity` lines still apply (verified 2026-10-16). Widen the analysis by adding rule-ID lines, not
  bulk or category lines.

- **Fix the code; do not suppress.** No `#pragma warning disable`, `[SuppressMessage]`, per-file severity override or
  CA2000 option change (`dispose_ownership_transfer_*`, `excluded_*`) for these rules. A real false positive is a discussion
  with the owner first.
- Dispose with `using` / `await using`. When ownership moves to something that disposes it later (a `FileStreamResult`, a
  `MultipartFormDataContent` part), use the try/finally hand-over: assign `null` right after the hand-over and dispose the
  non-null leftover in `finally` (see `FileController.Download`, `MultipartForm`).
- A class that owns a disposable field disposes it in `Dispose` / `DisposeAsync` (e.g. `EmailConsumerWorker.Dispose`),
  not only on a happy-path method such as `StopAsync`.
- A client from `IHttpClientFactory` is disposed after use (`using HttpClient`); it does not own its pooled handler, and a
  response it already returned (the streaming `OpenReadAsync`) keeps working.
- Tests: multipart bodies are built with the per-project `Fixtures/MultipartForm` helper (`Of`, `AddText`, `AddFile`),
  never `new StringContent(...)` / `new ByteArrayContent(...)` added inline; a helper that sends a request takes a
  `Func<...>` and disposes what it builds (`ErrorOf`, `Send`).
- A leak fix with observable behaviour gets a regression test like any bug fix (`FileClientTests.EveryCall_DisposesTheHttpClientItCreated`,
  `FileControllerTests.Download_ClosesTheFile_WhenItFailsAfterOpeningIt`); where nothing is observable, the build error is the red step.
