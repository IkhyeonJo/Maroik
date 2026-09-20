using System.Reflection;
using Maroik.Worker.Workers;
using Maroik.Tests.Shared;

namespace Maroik.Worker.Tests.Architecture;

/// <summary>Logging rules (literal templates, catch must log or rethrow) for the production assembly.</summary>
public class WorkerLoggingArchitectureTests : LoggingArchitectureTestsBase
{
    protected override Assembly ProductionAssembly => typeof(EmailConsumerWorker).Assembly;

    protected override IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } = new Dictionary<string, string>
    {
        ["Maroik.Worker.Workers.EmailConsumerWorker.ExecuteAsync"] =
            "OperationCanceledException from Task.Delay(Infinite, stoppingToken): the expected signal of a graceful shutdown.",
    };
}
