using System.Reflection;
using Maroik.Core.Repository.Repositories;
using NetArchTest.Rules;

namespace Maroik.Core.Repository.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Core.Repository, Maroik.Core.Client, and
/// Maroik.Core.Service remain sibling layers built on Maroik.Core.Contract and must
/// not reference each other.
/// </summary>
public class RepositoryArchitectureTests
{
    private static readonly Assembly RepositoryAssembly = typeof(AccountRepository).Assembly;

    [Fact]
    public void Repository_ShouldNot_DependOnClientNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(RepositoryAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Client")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Repository must not reference Maroik.Core.Client directly. " +
            "These are sibling infrastructure layers and must not depend on each other.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Repository_ShouldNot_DependOnServiceNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(RepositoryAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Service")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Repository must not reference Maroik.Core.Service directly. " +
            "Repository is an infrastructure layer and must not depend on the service layer.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
