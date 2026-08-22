using System.Reflection;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Filters;
using NetArchTest.Rules;

namespace Maroik.Website.Tests.Architecture;

/// <summary>
/// Architecture tests that enforce the layering rule for Maroik.Website:
///
///   Website  →  Service interfaces (Maroik.Core.Contract)
///                    ↓
///              Service implementations (Maroik.Core.Service)
///                    ↓
///              Repository + Client implementations
///
/// Website must only consume the service layer via interfaces declared in
/// Maroik.Core.Contract. Direct dependencies on Maroik.Core.Repository,
/// Maroik.Core.Client, or Maroik.Core.Service (the implementations) are
/// forbidden for all types except Program (the composition root), which
/// wires up implementations into the DI container.
/// </summary>
public class WebsiteArchitectureTests
{
    private static readonly Assembly WebsiteAssembly = typeof(ViewBagPopulatorFilter).Assembly;
    private static readonly Assembly ContractAssembly = typeof(IAccountRepository).Assembly;

    private const string RepositoryNamespace = "Maroik.Core.Repository";
    private const string ClientNamespace = "Maroik.Core.Client";
    private const string ServiceNamespace = "Maroik.Core.Service";

    /// <summary>
    /// Full names of every repository interface (<c>I*Repository</c>) declared in
    /// Maroik.Core.Contract.Interfaces. Namespace-based checks alone don't stop a Website type
    /// from injecting one of these directly — they live in Contract alongside service and
    /// client interfaces, so bypassing the service layer this way doesn't touch
    /// Maroik.Core.Repository at all and slips past the namespace-based rules above.
    /// </summary>
    private static readonly string[] RepositoryInterfaceFullNames =
    [
        .. ContractAssembly.GetTypes()
            .Where(t => t is { IsInterface: true, Namespace: "Maroik.Core.Contract.Interfaces" })
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(t => t.FullName!)
    ];

    /// <summary>Website types except composition root should not depend on repository namespace.</summary>
    [Fact]
    public void WebsiteTypes_ExceptCompositionRoot_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebsiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Website must not reference " +
            "Maroik.Core.Repository directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Website types except composition root should not depend on repository interfaces,
    /// even though those interfaces are declared in Maroik.Core.Contract (not
    /// Maroik.Core.Repository). Only Maroik.Core.Service may consume them.
    /// </summary>
    [Fact]
    public void WebsiteTypes_ExceptCompositionRoot_ShouldNot_DependOnRepositoryInterfaces()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebsiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOnAny(RepositoryInterfaceFullNames)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Website must not depend on repository " +
            "interfaces (I*Repository) directly. Even though these are declared in " +
            "Maroik.Core.Contract, only Maroik.Core.Service may consume them — inject a " +
            "service interface instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Website types except composition root should not depend on client namespace.</summary>
    [Fact]
    public void WebsiteTypes_ExceptCompositionRoot_ShouldNot_DependOnClientNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebsiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Website must not reference " +
            "Maroik.Core.Client directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Website types except composition root should not depend on service namespace.</summary>
    [Fact]
    public void WebsiteTypes_ExceptCompositionRoot_ShouldNot_DependOnServiceNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WebsiteAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(ServiceNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Website must not reference " +
            "Maroik.Core.Service implementations directly. " +
            "Use service interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Controllers should not depend on repository or client or service namespace.</summary>
    [Fact]
    public void Controllers_ShouldNot_DependOnRepositoryOrClientOrServiceNamespace()
    {
        NetArchTest.Rules.TestResult repoResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace("Maroik.Website.Controllers")
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        NetArchTest.Rules.TestResult clientResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace("Maroik.Website.Controllers")
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        NetArchTest.Rules.TestResult serviceResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace("Maroik.Website.Controllers")
            .ShouldNot()
            .HaveDependencyOn(ServiceNamespace)
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

        Assert.True(
            serviceResult.IsSuccessful,
            "Controllers must not reference Maroik.Core.Service implementations directly. " +
            "Inject IXxxService interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", serviceResult.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Razor views are compiled into classes under the <c>AspNetCoreGeneratedDocument</c>
    /// namespace. Views must only render the ViewModel/DTO already handed to them by the
    /// controller — they must not reach into Repository, Client, or Service implementations
    /// (e.g. via <c>@inject</c>) to fetch or mutate data themselves.
    /// </summary>
    [Fact]
    public void Views_ShouldNot_DependOnRepositoryOrClientOrServiceNamespace()
    {
        const string generatedViewNamespace = "AspNetCoreGeneratedDocument";

        NetArchTest.Rules.TestResult repoResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace(generatedViewNamespace)
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        NetArchTest.Rules.TestResult clientResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace(generatedViewNamespace)
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        NetArchTest.Rules.TestResult serviceResult = Types.InAssembly(WebsiteAssembly)
            .That()
            .ResideInNamespace(generatedViewNamespace)
            .ShouldNot()
            .HaveDependencyOn(ServiceNamespace)
            .GetResult();

        Assert.True(
            repoResult.IsSuccessful,
            "Views must not reference Maroik.Core.Repository directly (e.g. via @inject). " +
            "Pass all data through the controller's ViewModel instead.\n" +
            "Failing types: " + string.Join(", ", repoResult.FailingTypeNames ?? []));

        Assert.True(
            clientResult.IsSuccessful,
            "Views must not reference Maroik.Core.Client directly (e.g. via @inject). " +
            "Pass all data through the controller's ViewModel instead.\n" +
            "Failing types: " + string.Join(", ", clientResult.FailingTypeNames ?? []));

        Assert.True(
            serviceResult.IsSuccessful,
            "Views must not reference Maroik.Core.Service implementations directly (e.g. via @inject). " +
            "Pass all data through the controller's ViewModel instead.\n" +
            "Failing types: " + string.Join(", ", serviceResult.FailingTypeNames ?? []));
    }
}
