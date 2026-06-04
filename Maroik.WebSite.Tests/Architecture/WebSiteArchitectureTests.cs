using System.Reflection;
using Maroik.WebSite.Controllers;
using NetArchTest.Rules;

namespace Maroik.WebSite.Tests.Architecture;

/// <summary>
/// Architecture tests that enforce the layering rule for Maroik.WebSite:
///
///   WebSite  →  Service interfaces (Maroik.Core.Contract)
///                    ↓
///              Service implementations (Maroik.Core.Service)
///                    ↓
///              Repository + Client implementations
///
/// WebSite must only consume the service layer via interfaces declared in
/// Maroik.Core.Contract. Direct dependencies on Maroik.Core.Repository or
/// Maroik.Core.Client are forbidden for all types except Program (the
/// composition root), which wires up implementations into the DI container.
/// </summary>
public class WebSiteArchitectureTests
{
    private static readonly Assembly WebSiteAssembly = typeof(BaseController).Assembly;

    private const string RepositoryNamespace = "Maroik.Core.Repository";
    private const string ClientNamespace = "Maroik.Core.Client";

    [Fact]
    public void WebSiteTypes_ExceptCompositionRoot_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebSiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.WebSite must not reference " +
            "Maroik.Core.Repository directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void WebSiteTypes_ExceptCompositionRoot_ShouldNot_DependOnClientNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebSiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.WebSite must not reference " +
            "Maroik.Core.Client directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controllers_ShouldNot_DependOnRepositoryOrClientNamespace()
    {
        NetArchTest.Rules.TestResult repoResult = Types.InAssembly(WebSiteAssembly)
            .That()
            .ResideInNamespace("Maroik.WebSite.Controllers")
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        NetArchTest.Rules.TestResult clientResult = Types.InAssembly(WebSiteAssembly)
            .That()
            .ResideInNamespace("Maroik.WebSite.Controllers")
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        Assert.True(
            repoResult.IsSuccessful,
            "Controllers must not reference Maroik.Core.Repository directly. " +
            "Inject IXxxService interfaces instead.\n" +
            "Failing types: " + string.Join(", ", repoResult.FailingTypeNames ?? []));

        Assert.True(
            clientResult.IsSuccessful,
            "Controllers must not reference Maroik.Core.Client directly. " +
            "Inject IXxxService interfaces instead.\n" +
            "Failing types: " + string.Join(", ", clientResult.FailingTypeNames ?? []));
    }
}
