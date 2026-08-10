using System.Reflection;
using Maroik.Core.Client.Clients;
using NetArchTest.Rules;

namespace Maroik.Core.Client.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Core.Client, Maroik.Core.Repository, and
/// Maroik.Core.Service remain sibling layers built on Maroik.Core.Contract and must
/// not reference each other.
/// </summary>
public class ClientArchitectureTests
{
    private static readonly Assembly ClientAssembly = typeof(FileClient).Assembly;

    /// <summary>Client should not depend on repository namespace.</summary>
    [Fact]
    public void Client_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ClientAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Repository")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Client must not reference Maroik.Core.Repository directly. " +
            "These are sibling infrastructure layers and must not depend on each other.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Client should not depend on service namespace.</summary>
    [Fact]
    public void Client_ShouldNot_DependOnServiceNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ClientAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Service")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Client must not reference Maroik.Core.Service directly. " +
            "Client is an infrastructure layer and must not depend on the service layer.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
