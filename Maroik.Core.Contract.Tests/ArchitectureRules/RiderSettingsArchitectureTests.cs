using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// Rider reads the team-shared ReSharper settings from <c>&lt;solution file name&gt;.DotSettings</c> next to the solution. After the
/// move from <c>Maroik.sln</c> to <c>Maroik.slnx</c> the shared file kept its old name, <c>Maroik.sln.DotSettings</c>, and Rider
/// silently ignored it — the code style, the inspection severities and the spelling words in it applied to nobody — so its name is
/// pinned to the solution file's.
/// </summary>
public class RiderSettingsArchitectureTests
{
    /// <summary>The shared settings file is named after the solution file, and no file of another name is left beside it.</summary>
    [Fact]
    public void TheSharedRiderSettings_AreNamedAfterTheSolutionFile()
    {
        string[] settings = [.. Directory.GetFiles(RepositoryPaths.Root, "*.DotSettings").Select(Path.GetFileName).OfType<string>()];

        Assert.Equal(["Maroik.slnx.DotSettings"], settings);
        Assert.True(File.Exists(Path.Combine(RepositoryPaths.Root, "Maroik.slnx")), "Maroik.slnx is not at the repository root");
    }
}
