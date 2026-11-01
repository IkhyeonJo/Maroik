using System.Text.RegularExpressions;
using Maroik.Core.Contract.Tests.Fixtures;

namespace Maroik.Core.Contract.Tests.ArchitectureRules;

/// <summary>
/// The website's two test hosts — <c>MaroikWebApplicationFactory</c> (Maroik.Website.Tests) and
/// <c>PlaywrightWebApplicationFactory</c> (Maroik.E2E.Tests) — each list the configuration they start the site with. The two
/// projects share no code, so a setting changed in one list and not the other would make the integration and the E2E suites
/// test different sites without anyone noticing. Each may add keys of its own (E2E's ClamAV, the integration host's dummy
/// connection string, which E2E replaces with its container's); a key both list carries the same value.
/// </summary>
public class TestHostSettingsArchitectureTests
{
    private static readonly string _websiteFactory =
        Path.Combine(RepositoryPaths.Root, "Maroik.Website.Tests", "Fixtures", "MaroikWebApplicationFactory.cs");

    private static readonly string _e2eFactory =
        Path.Combine(RepositoryPaths.Root, "Maroik.E2E.Tests", "Fixtures", "PlaywrightWebApplicationFactory.cs");

    /// <summary>Every key both factories set to a literal has the same value in both.</summary>
    [Fact]
    public void TheTwoTestHosts_GiveTheirSharedSettingsTheSameValues()
    {
        Dictionary<string, string> website = LiteralSettings(File.ReadAllText(_websiteFactory));
        Dictionary<string, string> e2e = LiteralSettings(File.ReadAllText(_e2eFactory));

        List<string> differing =
        [
            .. website.Keys.Intersect(e2e.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)
                .Where(key => !string.Equals(website[key], e2e[key], StringComparison.Ordinal))
                .Select(key => $"{key}: \"{website[key]}\" vs \"{e2e[key]}\"")
        ];

        Assert.Empty(differing);
    }

    /// <summary>Both lists are found and share the site's core settings, so the check above compares something.</summary>
    [Fact]
    public void TheTwoTestHosts_ShareTheCoreSettings()
    {
        Dictionary<string, string> website = LiteralSettings(File.ReadAllText(_websiteFactory));
        Dictionary<string, string> e2e = LiteralSettings(File.ReadAllText(_e2eFactory));

        foreach (string key in new[] { "ServerSetting:DomainName", "ServerSetting:SessionExpireMinutes", "ServerSetting:MaxLoginAttempt", "ConnectionStrings:Valkey" })
        {
            Assert.True(website.ContainsKey(key), $"MaroikWebApplicationFactory does not set {key}");
            Assert.True(e2e.ContainsKey(key), $"PlaywrightWebApplicationFactory does not set {key}");
        }
    }

    /// <summary>A differing value and a non-literal value are told apart from the literal entries.</summary>
    [Fact]
    public void LiteralSettings_ReadsOnlyTheStringLiteralEntries()
    {
        const string source = """
            ["A:B"] = "1",
            ["C"] = someVariable,
            ["D"] = ""
            """;

        Dictionary<string, string> settings = LiteralSettings(source);

        Assert.Equal(new Dictionary<string, string> { ["A:B"] = "1", ["D"] = "" }, settings);
    }

    /// <summary>The <c>["Key"] = "value"</c> entries of a source file (an entry whose value is not a string literal is skipped).</summary>
    private static Dictionary<string, string> LiteralSettings(string source) =>
        Regex.Matches(source, "\\[\"([^\"]+)\"\\]\\s*=\\s*\"([^\"]*)\"", RegexOptions.None, TimeSpan.FromSeconds(1))
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value, StringComparer.Ordinal);
}
