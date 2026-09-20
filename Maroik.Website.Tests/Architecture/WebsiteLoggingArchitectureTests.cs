using System.Reflection;
using Maroik.Website.Filters;
using Maroik.Tests.Shared;

namespace Maroik.Website.Tests.Architecture;

/// <summary>Logging rules (literal templates, catch must log or rethrow) for the production assembly.</summary>
public class WebsiteLoggingArchitectureTests : LoggingArchitectureTestsBase
{
    protected override Assembly ProductionAssembly => typeof(ViewBagPopulatorFilter).Assembly;

    protected override IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } = new Dictionary<string, string>
    {
        ["Maroik.Website.HealthChecks.ValkeyHealthCheck.CheckHealthAsync"] =
            "Returns HealthCheckResult.Unhealthy carrying the exception; the health-check framework logs it and /health reports it.",
    };
}
