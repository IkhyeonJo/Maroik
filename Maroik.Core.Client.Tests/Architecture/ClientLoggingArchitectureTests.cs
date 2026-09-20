using System.Reflection;
using Maroik.Core.Client.Clients;
using Maroik.Tests.Shared;

namespace Maroik.Core.Client.Tests.Architecture;

/// <summary>Logging rules (literal templates, catch must log or rethrow) for the production assembly.</summary>
public class ClientLoggingArchitectureTests : LoggingArchitectureTestsBase
{
    protected override Assembly ProductionAssembly => typeof(FileClient).Assembly;

    protected override IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } = new Dictionary<string, string>
    {
        ["Maroik.Core.Client.HealthChecks.RabbitMqHealthCheck.CheckHealthAsync"] =
            "Returns HealthCheckResult.Unhealthy carrying the exception; the health-check framework logs it and /health reports it.",
    };
}
