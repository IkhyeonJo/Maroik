using System.Diagnostics;
using System.Text.RegularExpressions;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// The repository is spelled in American English, checked by cspell (<c>cspell.config.yaml</c>) as part of <c>dotnet test</c>,
/// so the spelling needs no step of its own in CI and no git hook.
/// <para>
/// The project's words live twice, because the two spell checkers cannot read each other's format: Rider reads the IntelliJ
/// project dictionary (<c>.idea/.idea.Maroik/.idea/dictionaries/project.xml</c>), and cspell reads a plain word list
/// (<c>.config/cspell-words.txt</c>, named by <c>cspell.config.yaml</c>). A word added to only one of them would pass in the
/// editor and fail here, or the other way round, so the two must hold the same words.
/// </para>
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

    private static readonly string _riderDictionary =
        Path.Combine(RepositoryPaths.Root, ".idea", ".idea.Maroik", ".idea", "dictionaries", "project.xml");

    private static readonly string _cspellWords = Path.Combine(RepositoryPaths.Root, ".config", "cspell-words.txt");

    /// <summary>Rider's project dictionary and cspell's word list hold exactly the same words.</summary>
    [Fact]
    public void RiderAndCspell_ShareOneWordList()
    {
        SortedSet<string> rider = new(
            Regex.Matches(File.ReadAllText(_riderDictionary), "<w>(.*?)</w>").Select(match => match.Groups[1].Value),
            StringComparer.Ordinal);
        SortedSet<string> cspell = new(
            File.ReadAllLines(_cspellWords).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')),
            StringComparer.Ordinal);

        Assert.NotEmpty(rider);
        Assert.Equal(string.Join("\n", rider), string.Join("\n", cspell));
    }

    /// <summary>
    /// The shared ReSharper settings hold no spelling words of their own (<c>UserDictionary</c>): a third list would drift from the
    /// two above unseen. A project word goes into the shared list, which Rider reads through its project dictionary.
    /// </summary>
    [Fact]
    public void TheSharedRiderSettings_KeepNoWordListOfTheirOwn()
    {
        string settings = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "Maroik.slnx.DotSettings"));

        Assert.DoesNotContain("/UserDictionary/", settings, StringComparison.Ordinal);
    }

    /// <summary>The cspell configuration reads that word list, so the shared words are the ones cspell uses.</summary>
    [Fact]
    public void CspellConfig_ReadsTheSharedWordList()
    {
        string config = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "cspell.config.yaml"));

        Assert.Contains("path: ./.config/cspell-words.txt", config, StringComparison.Ordinal);
    }

    /// <summary>
    /// cspell finds no unknown word in the repository. The files are the ones git does not ignore (tracked, or new and not yet
    /// added), filtered by the configuration's <c>files</c> globs and <c>ignorePaths</c>. A new word that is spelled right goes
    /// into both word lists (see <see cref="RiderAndCspell_ShareOneWordList"/>); the failure message lists every finding.
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
