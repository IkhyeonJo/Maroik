using System.Reflection;
using Maroik.Core.Domain.Account;
using NetArchTest.Rules;

namespace Maroik.Core.DomainTests.Architecture;

/// <summary>
/// Architecture tests enforcing that Maroik.Core.Domain is the innermost layer of the
/// application: it must not depend on any outer layer (Contract, Client, Repository,
/// Service, PostgreSQL, Website, Worker, FileStorage), and it must not depend on any
/// framework other than the BCL and the ErrorOr result-type library.
/// </summary>
public class DomainArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Account).Assembly;

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
        NetArchTest.Rules.TestResult result = Types.InAssembly(DomainAssembly)
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
        AssemblyName[] referenced = DomainAssembly.GetReferencedAssemblies();

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
    /// Bounded contexts recognized within Maroik.Core.Domain. Each one models a distinct
    /// area of the business (Account, Board, Calendar, Finance, Menu) and must stay
    /// independent of the others — cross-context collaboration, if ever needed, should go
    /// through an explicit integration pattern (e.g. a loosely-coupled reference such as
    /// Board.Writer being a plain string instead of an Account reference) rather than a
    /// direct type dependency.
    /// </summary>
    private static readonly string[] BoundedContextNamespaces =
    [
        "Maroik.Core.Domain.Account",
        "Maroik.Core.Domain.Board",
        "Maroik.Core.Domain.Calendar",
        "Maroik.Core.Domain.Finance",
        "Maroik.Core.Domain.Menu"
    ];

    /// <summary>Bounded context should not depend on another bounded context.</summary>
    [Theory]
    [InlineData("Maroik.Core.Domain.Account")]
    [InlineData("Maroik.Core.Domain.Board")]
    [InlineData("Maroik.Core.Domain.Calendar")]
    [InlineData("Maroik.Core.Domain.Finance")]
    [InlineData("Maroik.Core.Domain.Menu")]
    public void BoundedContext_ShouldNot_DependOnAnotherBoundedContext(string boundedContextNamespace)
    {
        string[] otherContexts = BoundedContextNamespaces
            .Where(ns => ns != boundedContextNamespace)
            .ToArray();

        NetArchTest.Rules.TestResult result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace(boundedContextNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(otherContexts)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Types in {boundedContextNamespace} must not depend on another bounded context " +
            $"({string.Join(", ", otherContexts)}). Bounded contexts must stay independent; " +
            "use a loosely-coupled reference (e.g. a plain identifier/string) instead of a " +
            "direct type dependency if cross-context data is needed.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
