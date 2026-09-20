using Maroik.Core.Repository.Repositories;
using Maroik.Tests.Shared;
using NetArchTest.Rules;

namespace Maroik.Core.Repository.Tests.Architecture;

/// <summary>
/// Logging rules for the persistence layer (see CLAUDE.md → "Logging"): Repository does NOT log — a failure
/// propagates to the service that owns the transaction, which logs it once where it is handled. So every
/// <c>catch</c> here must rethrow, or be listed below with the reason it is safe to swallow.
/// </summary>
public class RepositoryLoggingArchitectureTests
{
    private static readonly System.Reflection.Assembly _repositoryAssembly = typeof(AccountRepository).Assembly;

    private static readonly Dictionary<string, string> _allowedSilentCatches = new()
    {
        ["Maroik.Core.Repository.Repositories.UnitOfWork.RollbackAsync"] =
            "Rolling back a transaction that is already rolled back / committed throws; that is the expected no-op. The caller is already handling the original failure.",
        ["Maroik.Core.Repository.Repositories.UnitOfWork.DisposeAsync"] =
            "Best-effort rollback on dispose: the transaction may already be finished. Nothing more can be done in a disposer.",
        ["Maroik.Core.Repository.HealthChecks.DatabaseHealthCheck.CheckHealthAsync"] =
            "Returns HealthCheckResult.Unhealthy carrying the exception; the health-check framework logs it and /health reports it.",
    };

    [Fact]
    public void Repository_ShouldNot_LogItself()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_repositoryAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Extensions.Logging")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Maroik.Core.Repository must not log: let the exception propagate to the service that owns the transaction.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Repository_CatchBlocks_MustRethrow_OrBeAllowListedWithAReason()
    {
        string[] silent = [.. LoggingRules.SilentCatchesIn(LoggingRules.SourceFilesOf(_repositoryAssembly)).Distinct().Order()];

        string[] unexpected = [.. silent.Where(k => !_allowedSilentCatches.ContainsKey(k))];
        string[] stale = [.. _allowedSilentCatches.Keys.Where(k => !silent.Contains(k))];

        Assert.True(unexpected.Length == 0, "A catch that neither rethrows nor is allow-listed: " + string.Join(", ", unexpected));
        Assert.True(stale.Length == 0, "Stale allow-list entries: " + string.Join(", ", stale));
    }
}
