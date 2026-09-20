using System.Reflection;
using Maroik.FileStorage.Controllers;
using Maroik.Tests.Shared;

namespace Maroik.FileStorage.Tests.Architecture;

/// <summary>Logging rules (literal templates, catch must log or rethrow) for the production assembly.</summary>
public class FileStorageLoggingArchitectureTests : LoggingArchitectureTestsBase
{
    protected override Assembly ProductionAssembly => typeof(FileController).Assembly;

    protected override IReadOnlyDictionary<string, string> AllowedSilentCatches { get; } = new Dictionary<string, string>
    {
        ["Maroik.FileStorage.Helpers.StoragePath.ResolveWithinRoot"] =
            "Pure helper: an unresolvable path yields null; its two callers (FileController, FileValidationService results) log the refusal with the path.",
    };
}
