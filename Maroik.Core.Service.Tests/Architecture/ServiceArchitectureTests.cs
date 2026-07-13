using System.Reflection;
using Maroik.Core.Service.Services;
using NetArchTest.Rules;

namespace Maroik.Core.Service.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Core.Service does not reach past
/// Maroik.Core.Contract into the Client or Repository infrastructure layers.
/// </summary>
public class ServiceArchitectureTests
{
    private static readonly Assembly ServiceAssembly = typeof(AccountService).Assembly;

    [Fact]
    public void Service_ShouldNot_DependOnClientNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ServiceAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Client")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Service must not reference Maroik.Core.Client directly. " +
            "Depend on abstractions in Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Service_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ServiceAssembly)
            .ShouldNot()
            .HaveDependencyOn("Maroik.Core.Repository")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Core.Service must not reference Maroik.Core.Repository directly. " +
            "Depend on abstractions in Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
