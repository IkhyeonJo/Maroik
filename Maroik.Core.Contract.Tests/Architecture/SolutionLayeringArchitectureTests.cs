using System.Xml.Linq;

namespace Maroik.Core.Contract.Tests.Architecture;

/// <summary>
/// Solution-wide architecture validation. Where the per-layer <c>*ArchitectureTests</c> classes
/// each guard one assembly by reflecting over its compiled types, this class enforces the
/// <b>project dependency graph itself</b> — the <c>&lt;ProjectReference&gt;</c> and
/// <c>&lt;PackageReference&gt;</c> edges declared in every production <c>.csproj</c> — so a
/// forbidden edge fails the build the moment it is added to a project file, before any code is
/// written against it.
///
/// It encodes the ArchNet / Clean-Architecture rules for Maroik:
/// <list type="number">
///   <item>Domain and Contract are completely pure — BCL + the <c>ErrorOr</c> result type only
///         (Contract additionally depends on Domain). No other package, no other project.</item>
///   <item>Client / Repository (infrastructure) never reach another concrete layer: they build on
///         Contract abstractions and must not reference each other or Service, and external-system
///         packages (EF Core/Npgsql, MailKit, RabbitMQ) stay confined to the layer that owns them.</item>
///   <item>Service orchestrates over Contract abstractions only — it must not project-reference any
///         infrastructure/persistence project.</item>
///   <item>Dependencies flow strictly inward: no core project (Domain, Contract, Service, Client,
///         Repository, PostgreSQL) may reference an application host (Website, Worker, FileStorage),
///         and the whole graph is acyclic.</item>
/// </list>
/// Every rule here is an xUnit assertion, so a violation fails <c>dotnet test</c> in CI (see
/// <c>.github/workflows/build-and-test.yml</c>) and blocks the PR.
/// </summary>
public class SolutionLayeringArchitectureTests
{
    // ── Layer names (project = assembly name, minus the .csproj) ──────────────────────────────
    private const string Domain = "Maroik.Core.Domain";
    private const string Contract = "Maroik.Core.Contract";
    private const string PostgreSql = "Maroik.Core.PostgreSQL";
    private const string Client = "Maroik.Core.Client";
    private const string Repository = "Maroik.Core.Repository";
    private const string Service = "Maroik.Core.Service";
    private const string Website = "Maroik.Website";
    private const string Worker = "Maroik.Worker";
    private const string FileStorage = "Maroik.FileStorage";

    /// <summary>The six inner layers. None of them may depend on an application host project.</summary>
    private static readonly string[] CoreLayers =
        [Domain, Contract, PostgreSql, Client, Repository, Service];

    /// <summary>The three composition-root / host projects that wire everything together.</summary>
    private static readonly string[] ApplicationHosts = [Website, Worker, FileStorage];

    /// <summary>
    /// The only <c>&lt;ProjectReference&gt;</c> edges allowed out of each production project.
    /// Anything a project references that is not in its set here is an architecture violation.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>
        {
            [Domain] = [],
            [Contract] = [Domain],
            [PostgreSql] = [],
            [Client] = [Contract],
            [Repository] = [Contract, PostgreSql],
            [Service] = [Contract],
            [Website] = [Contract, Client, Repository, Service],
            [Worker] = [Contract, Client, Repository, Service],
            [FileStorage] = [Contract, Client],
        };

    /// <summary>
    /// External-system packages and the only projects allowed to reference them directly. Keeping
    /// EF Core in Repository, the SMTP client in Client, and the message-broker client in Client
    /// (publisher) + Worker (consumer host) is what stops another layer from talking to an external
    /// system without going through a Contract interface.
    /// </summary>
    private static readonly (string PackagePrefix, string[] AllowedProjects, string Purpose)[] ConfinedPackages =
    [
        ("Microsoft.EntityFrameworkCore", [Repository, PostgreSql], "EF Core persistence"),
        ("Npgsql", [Repository, PostgreSql], "PostgreSQL driver"),
        ("MailKit", [Client], "SMTP mail transport"),
        ("RabbitMQ.Client", [Client, Worker], "RabbitMQ message broker"),
    ];

    private static readonly Lazy<IReadOnlyList<ProjectFile>> ProductionProjects =
        new(LoadProductionProjects);

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Rule 1 — Domain and Contract are completely pure
    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Domain is the innermost layer: no project reference at all, and no NuGet package
    /// except <c>ErrorOr</c> (the shared lightweight result/error object).</summary>
    [Fact]
    public void Domain_MustHaveNoProjectReferences_AndOnlyErrorOrPackage()
    {
        ProjectFile domain = GetProject(Domain);

        Assert.True(
            domain.ProjectReferences.Count == 0,
            $"{Domain} must not have any <ProjectReference>. The domain layer is the core of the " +
            "onion and depends on nothing.\nDeclared: " + string.Join(", ", domain.ProjectReferences));

        AssertOnlyErrorOrPackage(domain);
    }

    /// <summary>Contract is a pure abstraction layer: it may reference Domain and nothing else,
    /// and — like Domain — no NuGet package except <c>ErrorOr</c>.</summary>
    [Fact]
    public void Contract_MustReferenceOnlyDomain_AndOnlyErrorOrPackage()
    {
        ProjectFile contract = GetProject(Contract);

        Assert.True(
            contract.ProjectReferences.SetEquals([Domain]),
            $"{Contract} must reference exactly one project: {Domain}. " +
            "Contract holds interfaces + DTOs over the domain and nothing else.\nDeclared: " +
            string.Join(", ", contract.ProjectReferences));

        AssertOnlyErrorOrPackage(contract);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Rule 2 — infrastructure never bypasses Contract to reach another concrete layer
    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Client and Repository are sibling infrastructure layers. Each builds only on
    /// Contract (Repository also on the EF Core model project) — never on each other, never on
    /// Service, never on an application host.</summary>
    [Fact]
    public void InfrastructureLayers_MustBuildOnContractOnly_NotOnEachOtherOrService()
    {
        AssertReferencesAreExactly(Client, Contract);
        AssertReferencesAreExactly(Repository, Contract, PostgreSql);
    }

    /// <summary>External-system client packages stay in the one layer that owns that system, so no
    /// other layer can call the external system directly instead of through a Contract interface.</summary>
    [Fact]
    public void ExternalSystemPackages_MustBeConfinedToTheirOwningLayer()
    {
        List<string> violations = [];

        foreach ((string packagePrefix, string[] allowedProjects, string purpose) in ConfinedPackages)
        {
            foreach (ProjectFile project in ProductionProjects.Value)
            {
                bool referencesPackage = project.PackageReferences
                    .Any(p => p.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase));

                if (referencesPackage && !allowedProjects.Contains(project.Name))
                    violations.Add(
                        $"{project.Name} references '{packagePrefix}*' ({purpose}) — only " +
                        $"{string.Join(" / ", allowedProjects)} may. Depend on the matching " +
                        "Maroik.Core.Contract interface instead.");
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Rule 3 — Service orchestrates over abstractions only
    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The service layer is the sole orchestrator of business logic, and it does that job
    /// purely against Contract abstractions: it must not project-reference Client, Repository or the
    /// EF Core model project.</summary>
    [Fact]
    public void ServiceLayer_MustNotProjectReference_AnyInfrastructureOrPersistenceProject()
    {
        AssertReferencesAreExactly(Service, Contract);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Rule 4 — dependencies flow strictly inward, and the graph is acyclic
    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>No inner layer may reference an application host project (Website / Worker /
    /// FileStorage). Composition happens at the outside of the onion, never the inside.</summary>
    [Fact]
    public void CoreLayers_MustNotReference_AnyApplicationHostProject()
    {
        List<string> violations = [];

        foreach (string layer in CoreLayers)
        {
            string[] offending = GetProject(layer).ProjectReferences
                .Where(ApplicationHosts.Contains)
                .ToArray();

            if (offending.Length > 0)
                violations.Add($"{layer} references application host(s): {string.Join(", ", offending)}");
        }

        Assert.True(
            violations.Count == 0,
            "Inner layers must not depend on an application host — dependencies flow inward only.\n" +
            string.Join("\n", violations));
    }

    /// <summary>Every production project's <c>&lt;ProjectReference&gt;</c> set is a subset of the
    /// inward edges allowed for its layer. This is the catch-all that keeps the dependency graph
    /// exactly as designed.</summary>
    [Fact]
    public void EveryProductionProject_MustOnlyReference_ItsAllowedInwardProjects()
    {
        List<string> violations = [];

        foreach (ProjectFile project in ProductionProjects.Value)
        {
            if (!AllowedProjectReferences.TryGetValue(project.Name, out string[]? allowed))
            {
                violations.Add(
                    $"{project.Name} is a production project with no entry in AllowedProjectReferences — " +
                    "add it to the map (and to Maroik.sln's layer model) deliberately.");
                continue;
            }

            string[] forbidden = project.ProjectReferences.Except(allowed).ToArray();
            if (forbidden.Length > 0)
                violations.Add(
                    $"{project.Name} references {string.Join(", ", forbidden)} — allowed inward edges " +
                    $"are: {(allowed.Length == 0 ? "(none)" : string.Join(", ", allowed))}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    /// <summary>The project-reference graph over all Maroik projects (production + test) must be a
    /// DAG — a cycle would break the "dependencies flow one direction" rule outright.</summary>
    [Fact]
    public void ProjectReferenceGraph_MustBeAcyclic()
    {
        Dictionary<string, string[]> graph = LoadAllProjects()
            .ToDictionary(p => p.Name, p => p.ProjectReferences.ToArray());

        var visiting = new HashSet<string>();
        var done = new HashSet<string>();
        var cycle = new List<string>();

        bool HasCycle(string node)
        {
            if (done.Contains(node)) return false;
            if (!visiting.Add(node))
            {
                cycle.Add(node);
                return true;
            }

            if (graph.TryGetValue(node, out string[]? deps))
            {
                foreach (string dep in deps)
                {
                    if (HasCycle(dep))
                    {
                        cycle.Add(node);
                        return true;
                    }
                }
            }

            visiting.Remove(node);
            done.Add(node);
            return false;
        }

        foreach (string node in graph.Keys)
        {
            if (HasCycle(node))
            {
                cycle.Reverse();
                Assert.Fail("Project-reference cycle detected: " + string.Join(" -> ", cycle));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────────────────

    private static void AssertOnlyErrorOrPackage(ProjectFile project)
    {
        string[] unexpected = project.PackageReferences
            .Where(p => p != "ErrorOr")
            .ToArray();

        Assert.True(
            unexpected.Length == 0,
            $"{project.Name} must not reference any NuGet package except ErrorOr (the shared " +
            "result/error object). Unexpected: " + string.Join(", ", unexpected));
    }

    private static void AssertReferencesAreExactly(string projectName, params string[] expected)
    {
        ProjectFile project = GetProject(projectName);

        Assert.True(
            project.ProjectReferences.SetEquals(expected),
            $"{projectName} must project-reference exactly: {string.Join(", ", expected)}. " +
            $"Declared: {(project.ProjectReferences.Count == 0 ? "(none)" : string.Join(", ", project.ProjectReferences))}. " +
            "Reach every other concern through a Maroik.Core.Contract interface instead.");
    }

    private static ProjectFile GetProject(string name) =>
        ProductionProjects.Value.SingleOrDefault(p => p.Name == name)
        ?? throw new InvalidOperationException(
            $"Production project '{name}' not found under the repository root. " +
            "Known: " + string.Join(", ", ProductionProjects.Value.Select(p => p.Name)));

    private static IReadOnlyList<ProjectFile> LoadProductionProjects() =>
        LoadAllProjects()
            .Where(p => !p.Name.EndsWith(".Tests", StringComparison.Ordinal))
            .Where(p => p.Name != "Maroik.E2E.Tests")
            .ToArray();

    private static IReadOnlyList<ProjectFile> LoadAllProjects()
    {
        string root = FindRepositoryRoot();

        return Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !PathHasSegment(path, "bin") && !PathHasSegment(path, "obj"))
            .Where(path => Path.GetFileName(path).StartsWith("Maroik.", StringComparison.Ordinal))
            .Select(ProjectFile.Load)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool PathHasSegment(string path, string segment) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains(segment, StringComparer.OrdinalIgnoreCase);

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Maroik.sln")))
            dir = dir.Parent;
        if (dir == null)
            throw new InvalidOperationException(
                "Could not locate the repository root (Maroik.sln) from " + AppContext.BaseDirectory);
        return dir.FullName;
    }

    /// <summary>A parsed <c>.csproj</c>: its assembly name and its declared reference edges.</summary>
    private sealed record ProjectFile(
        string Name,
        HashSet<string> ProjectReferences,
        HashSet<string> PackageReferences)
    {
        public static ProjectFile Load(string path)
        {
            XDocument doc = XDocument.Load(path);

            HashSet<string> projectRefs = doc.Descendants()
                .Where(e => e.Name.LocalName == "ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => Path.GetFileNameWithoutExtension(v!.Replace('\\', '/')))
                .ToHashSet(StringComparer.Ordinal);

            HashSet<string> packageRefs = doc.Descendants()
                .Where(e => e.Name.LocalName is "PackageReference" or "PackageVersion")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .ToHashSet(StringComparer.Ordinal);

            return new ProjectFile(Path.GetFileNameWithoutExtension(path), projectRefs, packageRefs);
        }
    }
}
