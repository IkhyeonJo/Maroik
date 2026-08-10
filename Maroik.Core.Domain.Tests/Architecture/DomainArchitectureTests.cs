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
}
