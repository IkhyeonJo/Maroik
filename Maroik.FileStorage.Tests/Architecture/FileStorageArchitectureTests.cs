using System.Reflection;
using System.Runtime.CompilerServices;
using Maroik.FileStorage.Controllers;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Maroik.FileStorage.Tests.Architecture;

/// <summary>
/// Architecture tests enforcing that Maroik.FileStorage stays a standalone microservice, reached
/// only over its HTTP API, rather than acquiring a direct dependency on the monolith's internal
/// layers.
/// </summary>
public class FileStorageArchitectureTests
{
    private static readonly Assembly FileStorageAssembly = typeof(FileController).Assembly;

    /// <summary>FileStorage should not depend on the monolith's internal layers.</summary>
    [Theory]
    [InlineData("Maroik.Core.Service")]
    [InlineData("Maroik.Core.Repository")]
    [InlineData("Maroik.Core.PostgreSQL")]
    [InlineData("Maroik.Website")]
    public void FileStorage_ShouldNot_DependOnMonolithInternalLayers(string forbiddenNamespace)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(FileStorageAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Maroik.FileStorage must not reference {forbiddenNamespace}. " +
            "FileStorage is a standalone microservice reached only over its HTTP API " +
            "(see Maroik.Core.Client.Clients.FileClient) and must not pull in the monolith's " +
            "internal layers directly.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Controllers must not declare non-public helper methods, mirroring the same rule for
    /// Maroik.Website (see WebsiteArchitectureTests.Controllers_ShouldNot_DeclareNonPublicMethods):
    /// every method the routing system can dispatch to should be a public action, and shared logic
    /// should live in a service instead of a controller-local helper.
    /// </summary>
    [Fact]
    public void Controllers_ShouldNot_DeclareNonPublicMethods()
    {
        List<string> violations = [];

        foreach (Type controllerType in FileStorageAssembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(t)))
        {
            List<string> nonPublicMethods =
            [
                .. controllerType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName) // exclude property/event accessors
                    .Where(m => m.GetCustomAttribute<CompilerGeneratedAttribute>() == null) // exclude lambda bodies etc.
                    .Select(m => m.Name)
            ];

            if (nonPublicMethods.Count > 0)
                violations.Add($"{controllerType.Name}: {string.Join(", ", nonPublicMethods)}");
        }

        Assert.True(
            violations.Count == 0,
            "Controllers must not declare non-public helper methods — move shared logic into a " +
            "service instead.\n" +
            "Violations: " + string.Join("; ", violations));
    }
}
