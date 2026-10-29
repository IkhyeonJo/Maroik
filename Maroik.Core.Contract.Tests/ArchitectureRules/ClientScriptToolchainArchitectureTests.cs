using System.Text.RegularExpressions;
using System.Xml.Linq;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// A local docker-compose debug run needs no Node on the developer's machine. The image build (<c>docker compose -f
/// docker-compose.debug.yml up --build</c>, Rider / Visual Studio "regular mode") compiles the client scripts and runs their
/// tests in the Dockerfile's <c>tsbuild</c> stage. The host build (Rider / Visual Studio "Fast Mode", a plain Debug
/// <c>dotnet build</c>) does the same in a throwaway container of that stage's Node image when the project has no
/// <c>node_modules</c> (<c>ClientScriptsInDocker</c> in <c>Maroik.Website.csproj</c>, <c>client-scripts-in-docker.sh</c>).
/// Both paths use one Node image, so a script that compiles and passes in one also does in the other and in production.
/// </summary>
public class ClientScriptToolchainArchitectureTests
{
    private static readonly string _websiteFolder = Path.Combine(RepositoryPaths.Root, "Maroik.Website");

    /// <summary>The image the Dockerfile's client-script stage starts from (<c>FROM node:22-alpine AS tsbuild</c>).</summary>
    private static readonly Regex _tsbuildStage = new(@"^FROM\s+(?<image>\S+)\s+AS\s+tsbuild\s*$", RegexOptions.Multiline);

    /// <summary>The <c>RUN_CLIENT_TESTS</c> build argument of a compose file (<c>RUN_CLIENT_TESTS : "true"</c>).</summary>
    private static readonly Regex _runClientTestsArg = new(@"^\s*RUN_CLIENT_TESTS\s*:\s*""(?<value>[^""]*)""", RegexOptions.Multiline);

    [Fact]
    public void TheDebugComposeBuild_RunsTheClientScriptTests()
    {
        string compose = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "docker-compose.debug.yml"));

        Match arg = _runClientTestsArg.Match(compose);

        Assert.True(arg.Success, "docker-compose.debug.yml does not set the RUN_CLIENT_TESTS build argument");
        Assert.Equal("true", arg.Groups["value"].Value);
    }

    [Fact]
    public void TheProductionComposeBuild_LeavesTheClientScriptTestsToCi()
    {
        string compose = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "docker-compose.prod.yml"));

        Assert.DoesNotMatch(_runClientTestsArg, compose);
    }

    [Fact]
    public void TheHostBuildsDockerFallback_UsesTheNodeImageOfTheDockerfile()
    {
        Match stage = _tsbuildStage.Match(File.ReadAllText(Path.Combine(_websiteFolder, "Dockerfile")));
        Assert.True(stage.Success, "Maroik.Website/Dockerfile has no tsbuild stage");

        string? fallbackImage = XDocument.Load(Path.Combine(_websiteFolder, "Maroik.Website.csproj"))
            .Descendants("ClientScriptNodeImage")
            .Select(e => e.Value.Trim())
            .SingleOrDefault();

        Assert.Equal(stage.Groups["image"].Value, fallbackImage);
    }

    [Fact]
    public void TheHostBuildsDockerFallback_RunsTheScriptThatTheProjectShips()
    {
        string csproj = File.ReadAllText(Path.Combine(_websiteFolder, "Maroik.Website.csproj"));

        Assert.Contains("sh client-scripts-in-docker.sh", csproj, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_websiteFolder, "client-scripts-in-docker.sh")));
    }
}
