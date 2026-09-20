using System.Reflection;
using Maroik.Core.Service.Services;
using Maroik.Tests.Shared;

namespace Maroik.Core.Service.Tests.Architecture;

/// <summary>Logging rules (literal templates, catch must log or rethrow) for Maroik.Core.Service.</summary>
public class ServiceLoggingArchitectureTests : LoggingArchitectureTestsBase
{
    protected override Assembly ProductionAssembly => typeof(AccountService).Assembly;

    protected override IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } = new Dictionary<string, string>
    {
        ["Maroik.Core.Service.Services.HtmlParserService.TransformImageAttributesAsync"] =
            "Drains sibling fetch tasks before disposing the semaphore; the first fault is already propagating (and logged by the caller).",
        ["Maroik.Core.Service.Services.RsaService.Verify"] =
            "A non-Base64 signature is simply an invalid signature (expected input, not an error); the caller logs the failed verification.",
    };
}
