using System.Reflection;
using System.Text.RegularExpressions;
using Maroik.Core.Domain.Tests.Fixtures;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetArchTest.Rules;
namespace Maroik.Core.Domain.Tests.ArchitectureRules;

/// <summary>
/// Architecture tests enforcing that Maroik.Core.Domain is the innermost layer of the
/// application: it must not depend on any outer layer (Contract, Client, Repository,
/// Service, PostgreSQL, Website, Worker, FileStorage), and it must not depend on any
/// framework other than the BCL and the ErrorOr result-type library.
/// </summary>
public class DomainArchitectureTests
{
    /// <summary>The Maroik.Core.Domain assembly inspected by these architecture rules.</summary>
    private static readonly Assembly _domainAssembly = typeof(Domain.Accounts.Account).Assembly;

    /// <summary>Domain should not depend on any other layer.</summary>
    [Theory]
    [InlineData("Maroik.Core.Contract")]
    [InlineData("Maroik.Core.Client")]
    [InlineData("Maroik.Core.Repository")]
    [InlineData("Maroik.Core.Service")]
    [InlineData("Maroik.Core.PostgreSQL")]
    [InlineData("Maroik.Website")]
    [InlineData("Maroik.Worker")]
    [InlineData("Maroik.FileStorage")]
    public void Domain_ShouldNot_DependOnAnyOtherLayer(string forbiddenNamespace)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Maroik.Core.Domain must not reference {forbiddenNamespace}. " +
            "The domain layer must have zero outward dependencies so it can be reused by " +
            "every other layer without creating a cycle.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Domain should only depend on system or error or.</summary>
    [Fact]
    public void Domain_ShouldOnly_DependOn_SystemOrErrorOr()
    {
        AssemblyName[] referenced = _domainAssembly.GetReferencedAssemblies();

        var unexpected = referenced
            .Where(a => a.Name is not null)
            .Where(a => !a.Name!.StartsWith("System", StringComparison.Ordinal))
            .Where(a => !a.Name!.StartsWith("netstandard", StringComparison.Ordinal))
            .Where(a => a.Name != "ErrorOr")
            .Select(a => a.Name)
            .ToList();

        Assert.True(
            unexpected.Count == 0,
            "Maroik.Core.Domain must only depend on the BCL and ErrorOr. " +
            "Unexpected assembly references: " + string.Join(", ", unexpected));
    }

    /// <summary>
    /// Domain must not touch any infrastructure or third-party library used elsewhere in the
    /// solution (EF Core, ASP.NET Core, MailKit, RabbitMQ, JSON/HTML/image libraries, DI and
    /// hosting abstractions, …). Business logic in the innermost layer stays framework-free; the
    /// single deliberate exception is <c>ErrorOr</c>, the lightweight <c>ErrorOr&lt;T&gt;</c> /
    /// <c>Error</c> result type used to report outcomes without throwing. This overlaps
    /// <see cref="Domain_ShouldOnly_DependOn_SystemOrErrorOr"/> (an assembly-reference check); it is
    /// kept separately so a violation names the exact library and the exact offending type.
    /// </summary>
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.Extensions.DependencyInjection")]
    [InlineData("Microsoft.Extensions.Configuration")]
    [InlineData("Microsoft.Extensions.Logging")]
    [InlineData("Microsoft.Extensions.Options")]
    [InlineData("Microsoft.Extensions.Hosting")]
    [InlineData("Microsoft.Extensions.Caching")]
    [InlineData("Npgsql")]
    [InlineData("MailKit")]
    [InlineData("MimeKit")]
    [InlineData("RabbitMQ.Client")]
    [InlineData("Newtonsoft.Json")]
    [InlineData("HtmlAgilityPack")]
    [InlineData("Ganss.Xss")]
    [InlineData("AngleSharp")]
    [InlineData("ImageMagick")]
    [InlineData("BCrypt.Net")]
    [InlineData("DocumentFormat.OpenXml")]
    [InlineData("ICSharpCode.SharpZipLib")]
    [InlineData("Serilog")]
    [InlineData("Swashbuckle")]
    [InlineData("NonFactors.Grid")]
    [InlineData("Dapper")]
    [InlineData("AutoMapper")]
    [InlineData("MediatR")]
    public void Domain_ShouldNot_DependOn_ExternalInfrastructureLibrary(string forbiddenNamespace)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Maroik.Core.Domain must not reference {forbiddenNamespace}. The domain layer must " +
            "stay pure: business rules, invariants and policies are plain C# with no framework or " +
            "third-party dependency (the sole exception is the ErrorOr result type).\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>The root namespace of the domain.</summary>
    private const string DomainNamespace = "Maroik.Core.Domain";

    /// <summary>The folder holding the domain's sources.</summary>
    private static readonly string _domainSources = Path.Combine(RepositoryPaths.Root, DomainNamespace);

    /// <summary>
    /// The bounded contexts of Maroik.Core.Domain, one folder each. Each models a distinct area of the business and stays
    /// independent of the others: where one needs another's data it keeps a plain identifier (Board.Writer is a nickname,
    /// not an Account) rather than a type of the other context.
    /// </summary>
    private static readonly string[] _boundedContexts =
        ["Accounts", "Boards", "Budgets", "Calendars", "FileManagers", "Finances", "Kanbans", "Menus"];

    /// <summary>The shared kernel: what every context may build on, and which itself depends on no context.</summary>
    private static readonly string[] _sharedKernel = ["Errors", "Extensions", "Formulas", "Primitives", "Uploads", "ValueObjects"];

    /// <summary>The dependencies between contexts that are intended: a budget is measured against the finance records.</summary>
    private static readonly Dictionary<string, string[]> _allowedContextDependencies = new() { ["Budgets"] = ["Finances"] };

    /// <summary>The contexts the sources of <paramref name="folder"/> may not depend on: every other one, the intended dependencies aside.</summary>
    private static string[] ForbiddenContextsFor(string folder) =>
        [.. _boundedContexts.Where(context => context != folder && !_allowedContextDependencies.GetValueOrDefault(folder, []).Contains(context))];

    /// <summary>A new folder has to be declared a context or part of the shared kernel, or the rules below would not see it.</summary>
    [Fact]
    public void EveryDomainFolder_IsABoundedContext_OrPartOfTheSharedKernel()
    {
        string[] expected = [.. _boundedContexts.Concat(_sharedKernel).Order(StringComparer.Ordinal)];
        string[] folders =
        [
            .. new DirectoryInfo(_domainSources).GetDirectories().Select(folder => folder.Name)
                .Where(name => name is not ("bin" or "obj")).Order(StringComparer.Ordinal)
        ];

        Assert.Equal(expected, folders);
    }

    /// <summary>Bounded context should not depend on another bounded context (the compiled types).</summary>
    [Theory]
    [InlineData("Accounts")]
    [InlineData("Boards")]
    [InlineData("Budgets")]
    [InlineData("Calendars")]
    [InlineData("FileManagers")]
    [InlineData("Finances")]
    [InlineData("Kanbans")]
    [InlineData("Menus")]
    public void BoundedContext_ShouldNot_DependOnAnotherBoundedContext(string boundedContext)
    {
        string[] otherContexts = [.. ForbiddenContextsFor(boundedContext).Select(context => $"{DomainNamespace}.{context}")];

        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.{boundedContext}")
            .ShouldNot()
            .HaveDependencyOnAny(otherContexts)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Types in {boundedContext} must not depend on another bounded context " +
            $"({string.Join(", ", otherContexts)}). Bounded contexts must stay independent; " +
            "use a loosely-coupled reference (e.g. a plain identifier/string) instead of a " +
            "direct type dependency if cross-context data is needed.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// The source-side half of <see cref="BoundedContext_ShouldNot_DependOnAnotherBoundedContext"/>. The compiler copies a
    /// <c>const</c> into the code that reads it, so a context using another's constant leaves nothing in the IL: Boards and Menus
    /// read the Accounts context's role names that way until 2026-11-10. No source of a context, and none of the shared kernel,
    /// names another context: not in a using, not qualified. Comments are documentation and may.
    /// </summary>
    [Fact]
    public void DomainSources_ShouldNot_NameAnotherBoundedContext()
    {
        List<string> offenders = [];
        foreach (string folder in _boundedContexts.Concat(_sharedKernel))
        {
            string contexts = string.Join('|', ForbiddenContextsFor(folder));
            Regex named = new($@"Maroik\.Core\.Domain\.(?:{contexts})\b|(?<![\w.])(?:Domain\.)?(?:{contexts})\.[A-Z]", RegexOptions.CultureInvariant);
            foreach (string file in Directory.EnumerateFiles(Path.Combine(_domainSources, folder), "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int index = 0; index < lines.Length; index++)
                {
                    int comment = lines[index].IndexOf("//", StringComparison.Ordinal);
                    string code = comment < 0 ? lines[index] : lines[index][..comment];
                    if (named.IsMatch(code))
                        offenders.Add($"{folder}/{Path.GetFileName(file)}:{index + 1}: {code.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "A bounded context (and the shared kernel) must not name another bounded context:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// The domain is pure (CLAUDE.md → "Logging"): it returns errors, it does not log. Logging belongs to
    /// the service / client / host layers that decide what an error means.
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_Log()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Extensions.Logging")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Maroik.Core.Domain must not depend on Microsoft.Extensions.Logging.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Every Domain error message must be built via <c>DomainError.Validation</c>/<c>Conflict</c>/
    /// <c>Failure</c>, never the raw <c>ErrorOr.Error</c> factories directly. <c>DomainError</c> is
    /// what attaches the English composite-format template + args to <c>Error.Metadata</c> so an outer
    /// layer can translate the message via resx instead of falling back to an already-baked,
    /// untranslatable sentence (see <c>Maroik.Core.Domain.Errors.DomainError</c>'s doc comment). A raw
    /// factory call bakes the sentence into <c>Error.Description</c> with no template; when the message
    /// carries a runtime value (e.g. the currency code in <see cref="Maroik.Core.Domain.ValueObjects.Money"/>'s
    /// currency-mismatch errors) no resx key can ever match it.
    /// </summary>
    [Fact]
    public void Domain_ErrorMessages_MustGoThrough_DomainError()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .That()
            .DoNotResideInNamespace("Maroik.Core.Domain.Errors")
            .Should()
            .MeetCustomRule(new DoesNotCallRawErrorFactory())
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Domain types must build errors via DomainError.Validation/Conflict/Failure, not the " +
            "raw ErrorOr.Error.Validation/Conflict/Failure/NotFound/Forbidden/Unexpected factories " +
            "directly — only Maroik.Core.Domain.Errors.DomainError is allowed to call those.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// The domain never reads the clock (<c>DateTime.UtcNow</c>/<c>Now</c>/<c>Today</c>,
    /// <c>DateTimeOffset.UtcNow</c>/<c>Now</c>) and never holds a <see cref="TimeProvider"/>: the
    /// current time is a <c>DateTime utcNow</c> argument that the calling use case reads once from its
    /// injected <see cref="TimeProvider"/>, so expiry and boundary rules are testable at exact instants
    /// and one operation cannot stamp two different times.
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_ReadTheClock()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(_domainAssembly)
            .Should()
            .MeetCustomRule(new DoesNotReadTheClock())
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Maroik.Core.Domain must take the current time as a DateTime argument instead of reading " +
            "DateTime/DateTimeOffset.UtcNow/Now/Today or using TimeProvider.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// <paramref name="type"/> and every type nested in it. NetArchTest hands a custom rule only the
    /// top-level type, but lambdas, iterators and async methods compile their bodies into nested types,
    /// so an IL rule that looks only at <c>type.Methods</c> silently misses them.
    /// </summary>
    private static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
        => [type, .. type.NestedTypes.SelectMany(Flatten)];

    /// <summary>
    /// Flags a type whose IL reads the system clock through <see cref="DateTime"/> /
    /// <see cref="DateTimeOffset"/> or calls into <see cref="TimeProvider"/>.
    /// </summary>
    private sealed class DoesNotReadTheClock : ICustomRule
    {
        /// <summary>The clock-reading property getters, as "declaring type::method".</summary>
        private static readonly string[] _clockGetters =
        [
            "System.DateTime::get_UtcNow", "System.DateTime::get_Now", "System.DateTime::get_Today",
            "System.DateTimeOffset::get_UtcNow", "System.DateTimeOffset::get_Now",
        ];

        /// <summary>Returns <see langword="false"/> when any method body of <paramref name="type"/> (or a type nested in it) reads the clock.</summary>
        public bool MeetsRule(TypeDefinition type)
            => !Flatten(type).SelectMany(t => t.Methods).Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .Any(instruction => instruction.Operand is MethodReference called
                    && (_clockGetters.Contains($"{called.DeclaringType.FullName}::{called.Name}")
                        || called.DeclaringType.FullName == "System.TimeProvider"));
    }

    /// <summary>
    /// Flags a type whose IL directly calls one of the raw <c>ErrorOr.Error</c> factory methods.
    /// Deliberately checked at the method-call level via Mono.Cecil rather than as a type-dependency
    /// (<c>HaveDependencyOn("ErrorOr.Error")</c>) rule: every Domain type legitimately references the
    /// <c>ErrorOr&lt;T&gt;</c> return type, whose full name (<c>ErrorOr.ErrorOr`1</c>) shares the
    /// <c>"ErrorOr.Error"</c> prefix, so a dependency-based check would also flag that unrelated,
    /// required reference.
    /// </summary>
    private sealed class DoesNotCallRawErrorFactory : ICustomRule
    {
        /// <summary>The raw <c>ErrorOr.Error</c> factory methods domain code must not call directly.</summary>
        private static readonly string[] _rawFactoryMethodNames =
            ["Validation", "Conflict", "Failure", "NotFound", "Forbidden", "Unexpected"];

        /// <summary>Returns <see langword="false"/> when any method body of <paramref name="type"/> (or a type nested in it) calls a raw <c>ErrorOr.Error</c> factory.</summary>
        public bool MeetsRule(TypeDefinition type)
        {
            foreach (var instruction in Flatten(type).SelectMany(t => t.Methods).Where(method => method.HasBody).SelectMany(method => method.Body.Instructions.Where(instruction => instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)))
            {
                if (instruction.Operand is not MethodReference calledMethod) continue;

                if (calledMethod.DeclaringType.FullName == "ErrorOr.Error"
                    && _rawFactoryMethodNames.Contains(calledMethod.Name))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
