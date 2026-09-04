using System.Reflection;
using Maroik.Core.Service.Services;
using NetArchTest.Rules;

namespace Maroik.Core.Service.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Core.Service does not reach past
/// Maroik.Core.Contract into any infrastructure layer or outer application, and stays a sibling
/// of — not a dependent of — every other layer that itself depends on Service.
/// </summary>
public class ServiceArchitectureTests
{
    private static readonly Assembly ServiceAssembly = typeof(AccountService).Assembly;

    /// <summary>Service should not depend on any infrastructure layer or outer application.</summary>
    [Theory]
    [InlineData("Maroik.Core.Client")]
    [InlineData("Maroik.Core.Repository")]
    [InlineData("Maroik.Core.PostgreSQL")]
    [InlineData("Maroik.Website")]
    [InlineData("Maroik.Worker")]
    [InlineData("Maroik.FileStorage")]
    public void Service_ShouldNot_DependOnInfrastructureOrOuterLayer(string forbiddenNamespace)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ServiceAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Maroik.Core.Service must not reference {forbiddenNamespace} directly. " +
            "Depend on abstractions in Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
