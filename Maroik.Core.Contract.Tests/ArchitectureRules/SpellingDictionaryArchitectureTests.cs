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
}
