using System.Text.RegularExpressions;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// The project rules are split between <c>CLAUDE.md</c> (cross-cutting, always loaded) and <c>.claude/rules/*.md</c>
/// (one area each, loaded by Claude Code when a file matching the rule file's <c>paths:</c> globs is read or edited).
/// A rule file that no file matches any more — after a rename or a removed folder — would silently never load, and a rule
/// file missing from the CLAUDE.md index could not be found by a reader; these tests catch both.
/// </summary>
public class ClaudeRulesArchitectureTests
{
    private static readonly string _rulesDirectory = Path.Combine(RepositoryPaths.Root, ".claude", "rules");

    /// <summary>Folders that hold no source of this repository (build output, dependencies, git's own store).</summary>
    private static readonly string[] _ignoredSegments = ["bin", "obj", "node_modules", ".git", "TestResults"];

    private static readonly Lazy<List<string>> _repositoryFiles = new(() =>
    [
        .. Directory.EnumerateFiles(RepositoryPaths.Root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(RepositoryPaths.Root, path).Replace('\\', '/'))
            .Where(path => !_ignoredSegments.Any(segment => path.Split('/').Contains(segment, StringComparer.Ordinal)))
    ]);

    /// <summary>Every rule file's name (e.g. <c>calendar.md</c>).</summary>
    public static TheoryData<string> RuleFiles() =>
        [.. Directory.EnumerateFiles(_rulesDirectory, "*.md").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];

    /// <summary>Each <c>paths:</c> glob of a rule file matches at least one file of the repository.</summary>
    [Theory]
    [MemberData(nameof(RuleFiles))]
    public void EveryPathsGlob_MatchesAFileOfTheRepository(string ruleFile)
    {
        List<string> stale = [.. PathsGlobs(ruleFile).Where(glob => !_repositoryFiles.Value.Any(GlobRegex(glob).IsMatch))];

        Assert.True(stale.Count == 0, $"{ruleFile}: no file matches {string.Join(", ", stale)}");
    }

    /// <summary>A rule file with no <c>paths:</c> loads for every task, so it must be imported by CLAUDE.md instead.</summary>
    [Theory]
    [MemberData(nameof(RuleFiles))]
    public void EveryRuleFile_IsPathScoped_OrImportedByClaudeMd(string ruleFile)
    {
        bool imported = ClaudeMd().Contains($"@.claude/rules/{ruleFile}", StringComparison.Ordinal);

        Assert.True(imported || PathsGlobs(ruleFile).Count > 0, $"{ruleFile} has no paths: and CLAUDE.md does not import it");
    }

    /// <summary>CLAUDE.md names every rule file, so a reader (and a whole-repo review) can find all of them.</summary>
    [Theory]
    [MemberData(nameof(RuleFiles))]
    public void EveryRuleFile_IsNamedInClaudeMd(string ruleFile)
    {
        Assert.Contains($".claude/rules/{ruleFile}", ClaudeMd(), StringComparison.Ordinal);
    }

    /// <summary>Every rule file CLAUDE.md names exists.</summary>
    [Fact]
    public void EveryRuleFileClaudeMdNames_Exists()
    {
        List<string> missing =
        [
            .. Regex.Matches(ClaudeMd(), @"\.claude/rules/([A-Za-z0-9-]+\.md)").Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .Where(name => !File.Exists(Path.Combine(_rulesDirectory, name)))
        ];

        Assert.Empty(missing);
    }

    /// <summary>The globs translate as Claude Code reads them: <c>**</c> spans folders, <c>*</c> stays within one.</summary>
    [Theory]
    [InlineData("**/*Kanban*", "Maroik.Website/Controllers/KanbanController.cs", true)]
    [InlineData("**/*Kanban*", "KanbanNotes.md", true)]
    [InlineData("**/Kanbans/**", "Maroik.Core.Domain/Kanbans/KanbanCard.cs", true)]
    [InlineData("Maroik.Worker/**", "Maroik.Worker/Workers/EmailConsumerWorker.cs", true)]
    [InlineData("Maroik.Worker/**", "Maroik.Worker.Tests/WorkerHostTests.cs", false)]
    [InlineData("**/*.cs", "Maroik.Website/Program.cs", true)]
    [InlineData("**/*.cs", "Maroik.Website/Program.cshtml", false)]
    [InlineData(".env*", ".env.debug", true)]
    [InlineData(".env*", "Maroik.Website/.env.debug", false)]
    public void GlobRegex_MatchesLikeClaudeCode(string glob, string path, bool expected)
    {
        Assert.Equal(expected, GlobRegex(glob).IsMatch(path));
    }

    private static string ClaudeMd() => File.ReadAllText(Path.Combine(RepositoryPaths.Root, "CLAUDE.md"));

    /// <summary>The quoted entries of the YAML front matter's <c>paths:</c> list (empty when there is none).</summary>
    private static List<string> PathsGlobs(string ruleFile)
    {
        string[] lines = File.ReadAllLines(Path.Combine(_rulesDirectory, ruleFile));
        if (lines.Length == 0 || lines[0] != "---")
            return [];

        return
        [
            .. lines.Skip(1).TakeWhile(line => line != "---")
                .Select(line => Regex.Match(line, "^\\s+-\\s+\"([^\"]+)\"\\s*$"))
                .Where(match => match.Success)
                .Select(match => match.Groups[1].Value)
        ];
    }

    /// <summary>A path glob as a regex over repository-relative paths with '/' separators.</summary>
    private static Regex GlobRegex(string glob)
    {
        string pattern = Regex.Escape(glob)
            .Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal)
            .Replace(@"/\*\*", "/.*", StringComparison.Ordinal)
            .Replace(@"\*", "[^/]*", StringComparison.Ordinal);
        return new Regex($"^{pattern}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }
}
