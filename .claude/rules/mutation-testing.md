---
paths:
  - "Maroik.Core.Domain.Tests/**"
  - "Maroik.Core.Service.Tests/**"
---

# Mutation testing (on demand)

Coverage says code ran, not that a test would notice it breaking. Stryker.NET (not pinned in the repo — install it
globally, version 5.0.0) changes the production code and reports which changes NO test catches ("survivors"). Run it from the
test project's folder, with the Microsoft.Testing.Platform runner (this repo uses xUnit v3 / MTP; the default vstest runner reports a false 0 %):

    dotnet tool install -g dotnet-stryker --version 5.0.0   # once
    cd Maroik.Core.Domain.Tests
    dotnet stryker --test-runner mtp --project Maroik.Core.Domain.csproj --reporter json --reporter cleartext

Read the survivors, not just the score. Known limits of the MTP runner: mutants inside **static field initializers**
(`static readonly` tables such as the reserved nicknames or the class taxonomies) are reported as survived even when a test does fail
for them (apply the mutation by hand to check), and a few mutants are equivalent (an `>=` vs `>` that only differs at an
exact-`now` instant). Baseline (2026-10-13): `Maroik.Core.Domain` 95.73 % (the rest is those two categories), `Maroik.Core.Service` 95.57 % (as of
the 2026-10-05 survivor review, the survivors left are equivalent — `?? ""` fallbacks on a value that is never null, duplicated guards, ordering of groups that hold a
single row). Service tests assert Begin/Commit/Rollback, the exact `ErrorCode`/`ErrorKey`/`ErrorType`, and every `catch`'s log entry;
keep new Service tests to that standard.
