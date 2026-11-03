using System.Text.RegularExpressions;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// The project's spelling words live twice, because the two spell checkers cannot read each other's format: Rider reads the
/// IntelliJ project dictionary (<c>.idea/.idea.Maroik/.idea/dictionaries/project.xml</c>), and CI / the pre-commit hook run
/// cspell, which reads a plain word list (<c>.config/cspell-words.txt</c>, named by <c>cspell.config.yaml</c>). A word added
/// to only one of them would pass in the editor and fail in CI, or the other way round, so the two must hold the same words.
/// </summary>
public class SpellingDictionaryArchitectureTests
{
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

    /// <summary>The cspell configuration reads that word list, so the shared words are the ones cspell uses.</summary>
    [Fact]
    public void CspellConfig_ReadsTheSharedWordList()
    {
        string config = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "cspell.config.yaml"));

        Assert.Contains("path: ./.config/cspell-words.txt", config, StringComparison.Ordinal);
    }

    /// <summary>
    /// CI spell-checks the repository with one exact cspell version (an unpinned <c>npx cspell</c> would pick up each new release,
    /// whose dictionaries can turn a green commit red).
    /// </summary>
    [Fact]
    public void Ci_RunsAPinnedCspell()
    {
        string workflow = File.ReadAllText(Path.Combine(RepositoryPaths.Root, ".github", "workflows", "ci-cd.yml"));

        Assert.Single(_pinnedCspell.Matches(workflow));
    }

    /// <summary>
    /// The pre-commit hook (<c>.githooks/pre-commit</c>, enabled by <c>git config core.hooksPath .githooks</c>) runs the same cspell
    /// version as CI, so a commit the hook lets through is not refused by CI for spelling, and it is executable, or git skips it.
    /// </summary>
    [Fact]
    public void PreCommitHook_RunsTheSameCspellAsCi()
    {
        string hookPath = Path.Combine(RepositoryPaths.Root, ".githooks", "pre-commit");
        string workflow = File.ReadAllText(Path.Combine(RepositoryPaths.Root, ".github", "workflows", "ci-cd.yml"));

        string[] hookVersions = [.. _pinnedCspell.Matches(File.ReadAllText(hookPath)).Select(match => match.Groups["version"].Value).Distinct()];

        Assert.Equal([_pinnedCspell.Match(workflow).Groups["version"].Value], hookVersions);
        // A Windows checkout has no execute bit to look at; CI (Linux) checks it.
        if (!OperatingSystem.IsWindows())
            Assert.True((File.GetUnixFileMode(hookPath) & UnixFileMode.UserExecute) != 0, ".githooks/pre-commit is not executable");
    }

    /// <summary>A cspell run pinned to an exact version: <c>cspell@1.2.3</c>.</summary>
    private static readonly Regex _pinnedCspell = new(@"\bcspell@(?<version>\d+\.\d+\.\d+)\b", RegexOptions.None, TimeSpan.FromSeconds(1));
}
