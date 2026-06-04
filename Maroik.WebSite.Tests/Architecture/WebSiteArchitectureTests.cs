using System.Reflection;
using Maroik.WebSite.Controllers;
using NetArchTest.Rules;

namespace Maroik.WebSite.Tests.Architecture;

/// <summary>
/// Architecture tests that enforce the rule: Maroik.WebSite must only consume
/// the service layer (Maroik.Core.Service / Maroik.Core.Contract) and must
/// never take a direct dependency on Maroik.Core.Repository.
///
/// The intended layering is:
///   WebSite  →  Service (interfaces in Contract)  →  Repository  →  PostgreSQL
///
/// Bypassing the service layer by referencing Repository types directly would
/// break the separation of concerns and make the web layer harder to test.
/// </summary>
public class WebSiteArchitectureTests
{
    private static readonly Assembly WebSiteAssembly = typeof(BaseController).Assembly;

    [Fact]
    public void WebSiteTypes_ExceptCompositionRoot_ShouldNot_DependOnRepositoryNamespace()
    {
        // Program.cs is the composition root and legitimately wires up Repository
        // implementations into the DI container. All other types must stay behind
        // the service-interface boundary.
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebSiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Repository")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.WebSite must not reference " +
            "Maroik.Core.Repository directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controllers_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebSiteAssembly)
            .That()
            .ResideInNamespace("Maroik.WebSite.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Repository")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Controllers must not reference Maroik.Core.Repository directly. " +
            "Inject IXxxService interfaces instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
