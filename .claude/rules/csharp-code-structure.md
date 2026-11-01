---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.resx"
  - "Maroik.slnx"
  - "stylecop.json"
  - "**/*.globalconfig"
---

# C# code structure

## Folder names are plural nouns

The folder that directly holds a `.cs` file is a plural noun (`Accounts`, `Calendars`, `Fixtures`, `ArchitectureRules`,
`Mappers`), and its namespace matches it. A singular noun is pluralized (`Account` → `Accounts`); an adjective or a
mass / concept word is first replaced by a noun that has a plural (`Shared` → `Layouts`, `Infrastructure` → `Fixtures`,
`Architecture` → `ArchitectureRules`, `Messaging` → `Messages`, `Time` → `TimeZones`, `Media` → `Uploads`).
A `Resources/...` resx folder mirrors its class's folder, so it follows. Razor `Views/{Controller}` folders hold no `.cs` and
keep the controller name.

`.editorconfig` cannot see folder names, so the `CheckPluralFolderNames` target in `Directory.Build.props` warns at build
time (`MAROIK0001`) for a folder whose name does not end in `s`; the zero-warning rule makes it a gate. It covers
`Maroik.Core.PostgreSQL` too: EF Core's scaffold puts everything at the project root by default, and this project's folders
are the scaffold options, so re-scaffold with `--context-dir Contexts --output-dir Models`.
The namespace follows the folder path; `.editorconfig` makes a mismatch a build error (`IDE0130`), so a folder rename
cannot leave its namespace behind.

Interfaces a project defines live in `Interfaces/` (`Maroik.Core.Contract`, and the hosts' own: `Maroik.Website/Interfaces`,
`Maroik.Worker/Interfaces`, `Maroik.FileStorage/Interfaces`), constants such as the console `LogTemplates` in `Constants/`,
extension methods in `Extensions/` (`EnumExtensions.GetDescription`, the Domain's `DateTimeExtensions` too), and `*Mapper`
classes in `Mappers/`. `Interfaces/` holds only interfaces (a record a contract carries, such as `HtmlImgPatch`, is a `Dtos/`
type) and `Extensions/` only classes of extension members (a plain static helper is named for its job, e.g.
`Maroik.Website/Serializers/ClientJsonSerializer`). Enforced for every production project except the
`Maroik.Core.PostgreSQL` scaffold by `FolderRoleArchitectureTests` (`Maroik.Core.Contract.Tests`), which reads the sources
with the Roslyn parser. The same test holds the `*.Tests` projects to one rule: every type that is not a `*Tests` class (a
fake, a fixture, an assertion helper such as `ErrorAssert` or `RepositoryPaths`) lives in `Fixtures/`, so the folders that
mirror the production code hold only its tests. A test class covers one area and sits in that area's folder
(`Calendars/CalendarBoundaryTests`); only the cross-aggregate checks of `Maroik.Core.Domain.Tests/Invariants` (the clock, the
error contract) have no production folder of their own.

## One type per file, named after it

Every `.cs` file holds one top-level type (class, record, struct, interface, enum, delegate) and is named after it
(`AdminStanding.cs`, `MenuSnapshot.cs`, `SeededAccount.cs`). StyleCop.Analyzers is referenced for that alone: `.editorconfig`
sets SA1402 (one type per file; `stylecop.json` counts every kind of type, not only classes) and SA1649 (file name = first
type) to `error` by rule ID, and `stylecop.globalconfig` turns every other StyleCop rule off. That file uses category lines —
the one exception to "configure analyzers by rule ID only" below, which holds only while no `AnalysisLevel=latest-all` /
`AnalysisMode` is set — plus a rule-ID line for SA1201, which 1.2.0-beta.556 reports with no source location for C# 14
`extension` blocks (a location-less diagnostic follows only a rule-ID line in a global config). Nested types are fine, and
SA1649 does not judge a `partial` type, so `ProgramAccessor.cs` (`public partial class Program`) keeps its name.

## Aggregate model

Every domain type persisted independently through its own `I*Repository`
(`IGenericRepository<T>` — its own `CreateAsync`/`UpdateEntityAsync`, not a cascade
through a parent) is an `AggregateRoot<TId>`, never a bare `Entity<TId>`. This
includes `BoardComment`, `BoardAttachedFile`, `CalendarEventReminder`,
`CalendarEventAttachedFile`, `CalendarShared` and `OtherCalendar` — reclassified
from `Entity<T>` to `AggregateRoot<T>` (base class only; parent-reference fields
are plain IDs, not object references), the same criterion applied to
`Category`/`SubCategory` in commit `284dc52f`. Enforced by
`ContractArchitectureTests.EveryRepositoryInterface_MustPersist_AnAggregateRoot`.

## Domain error messages must go through DomainError

The domain speaks English only and does no translation. Every `Maroik.Core.Domain` error is built via
`Maroik.Core.Domain.Errors.DomainError` (`.Validation`/`.Conflict`/`.Failure`) — **never** call the raw
`ErrorOr.Error.Validation` / `.Conflict` / `.Failure` / `.NotFound` / `.Forbidden` / `.Unexpected` factories
directly outside `DomainError.cs` itself. `DomainError` keeps the English message template and its values
separately in `Error.Metadata` (`MessageTemplate` / `MessageArgs`), so the outer layers can translate it.
This applies even to a message with no runtime value to interpolate (pass zero `args`) — a message that
starts static and later gains an interpolated value must not be able to silently lose its template.

Why: a raw `Error.X(code, "some message")` bakes the description straight into `Error.Description`
with no composite-format template in `Error.Metadata`, so the Website's `IHtmlLocalizer` resx lookup
falls back to the baked (often English, sometimes value-specific) string instead of resolving a
`ko-KR` translation. This was the actual state of ~98 of 104 Domain error call sites, and for
`Money.Add`/`Subtract`'s currency-mismatch errors it was a live bug: a genuinely runtime currency
code was interpolated straight into the message, so no resx key could ever match it.

Enforced by `DomainArchitectureTests.Domain_ErrorMessages_MustGoThrough_DomainError`
(`Maroik.Core.Domain.Tests/ArchitectureRules/DomainArchitectureTests.cs`) — a NetArchTest custom rule that
inspects each type's IL for a direct call to a raw `ErrorOr.Error` factory method (a type-dependency
check doesn't work here since every Domain type legitimately depends on the unrelated `ErrorOr<T>`
return type). Keep it green; when a new error is added, add it via `DomainError`, and add the
matching resx entry — with a `{0}`/`{1}` composite-format key, not a baked value — to every controller
resx pair (`en-US` + `ko-KR`) that can surface it.

Cultures are a Website concern: the supported UI cultures and the culture → default time-zone pre-select
live in `Maroik.Website/Constants/CulturePolicy.cs`, not in the domain. `Maroik.Core.Domain.Extensions.DateTimeExtensions`
is time-zone conversion, not language handling.

## Service error keys must come from ServiceErrorKeys

The message key of a failed service result (`ServiceResult`, `LoginResult`, `RegisterResult`, `SummernoteUploadResult`)
is also the resx key the controller looks it up under, so it is written as a `Maroik.Core.Contract.Dtos.ServiceErrorKeys`
constant, never a literal (the machine error *code*, e.g. `"Board.NotFound"`, stays a literal). Keys a controller compares
against and answers itself (`"invalid-image"`, `"reset-password-invalid"`, ...) live in `ServiceErrorKeys.Signals` and are
used by both sides. Rewording a value means renaming its resx key in every controller pair in the same change.

Enforced by `ServiceErrorKeyArchitectureTests` (`Maroik.Core.Service.Tests`, no literal keys in the service sources) and
`ServiceErrorKeyResxTests` (`Maroik.Website.Tests`), which follows the calls from each controller into the services with
Mono.Cecil and fails when a key the controller can receive is missing from its `en-US` / `ko-KR` resx, or when a key is
reached from no controller at all.
