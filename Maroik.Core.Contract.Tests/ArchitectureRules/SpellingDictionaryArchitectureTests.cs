using System.Diagnostics;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// The repository is spelled in American English, checked by cspell (<c>cspell.config.mjs</c>) as part of <c>dotnet test</c>,
/// so the spelling needs no step of its own in CI and no git hook. The project's words live once, in Rider's project dictionary
/// (<c>.idea/.idea.Maroik/.idea/dictionaries/project.xml</c>), which the cspell configuration reads.
/// </summary>
public class SpellingDictionaryArchitectureTests
{
    /// <summary>
    /// The cspell run, pinned to one exact version: an unpinned <c>npx cspell</c> would pick up each new release, whose
    /// dictionaries can turn a green commit red.
    /// </summary>
    private const string Cspell = "cspell@10.4.0";

    /// <summary>The image cspell runs in on a machine without Node: the one the Website build compiles the client scripts in.</summary>
    private const string NodeImage = "node:22-alpine";

    /// <summary>
    /// The shared ReSharper settings hold no spelling words of their own (<c>UserDictionary</c>): a third list would drift from the
    /// two above unseen. A project word goes into Rider's project dictionary.
    /// </summary>
    [Fact]
    public void TheSharedRiderSettings_KeepNoWordListOfTheirOwn()
    {
        string settings = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "Maroik.slnx.DotSettings"));

        Assert.DoesNotContain("/UserDictionary/", settings, StringComparison.Ordinal);
    }

    /// <summary>
    /// cspell finds no unknown word in the repository. The files are the ones git does not ignore (tracked, or new and not yet
    /// added), filtered by the configuration's <c>files</c> globs and <c>ignorePaths</c>. A new word that is spelled right goes
    /// into Rider's project dictionary; the failure message lists every finding.
    /// </summary>
    [Fact]
    public async Task TheRepository_HasNoSpellingErrors()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (int gitExitCode, string files) = await RunAsync("git", ["ls-files", "--cached", "--others", "--exclude-standard"], null, ct);
        Assert.True(gitExitCode == 0, $"git ls-files failed:\n{files}");

        // The list is what git does not ignore already, and the node image has no git to read .gitignore with.
        string[] cspellArgs = [Cspell, "--no-progress", "--no-summary", "--no-color", "--no-gitignore", "--file-list", "stdin"];
        (int exitCode, string output) = OnPath("npx")
            ? await RunAsync("npx", ["--yes", .. cspellArgs], files, ct)
            : await RunAsync("docker",
                ["run", "--rm", "-i", "-e", "NPM_CONFIG_UPDATE_NOTIFIER=false", "-v", $"{RepositoryPaths.Root}:/repo:ro",
                 "-v", "maroik-cspell-npm-cache:/root/.npm", "-w", "/repo", NodeImage, "npx", "--yes", .. cspellArgs],
                files, ct);

        Assert.True(exitCode == 0, $"cspell found unknown words (exit code {exitCode}):\n{output}");
    }

    /// <summary>True when an executable named <paramref name="name"/> is in a folder of <c>PATH</c>.</summary>
    private static bool OnPath(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(folder => File.Exists(Path.Combine(folder, name)));

    /// <summary>Runs a command in the repository root, feeding it <paramref name="input"/>; returns its exit code and output.</summary>
    private static async Task<(int ExitCode, string Output)> RunAsync(string command, string[] arguments, string? input, CancellationToken ct)
    {
        var info = new ProcessStartInfo(command, arguments)
        {
            WorkingDirectory = RepositoryPaths.Root, UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        using Process process = Process.Start(info) ?? throw new InvalidOperationException($"{command} did not start");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> stderr = process.StandardError.ReadToEndAsync(ct);
        await process.StandardInput.WriteAsync(input.AsMemory(), ct);
        process.StandardInput.Close();
        await process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromMinutes(5), ct);
        return (process.ExitCode, await stdout + await stderr);
    }
}
