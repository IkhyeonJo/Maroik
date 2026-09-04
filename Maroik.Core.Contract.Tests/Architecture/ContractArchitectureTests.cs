using System.Reflection;
using Maroik.Core.Contract.Interfaces;
using NetArchTest.Rules;

namespace Maroik.Core.Contract.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Core.Contract stays a pure abstraction layer:
/// interfaces and DTOs built on Maroik.Core.Domain only, with zero dependency on any concrete
/// implementation layer. Every other layer depends on Contract, so an accidental dependency
/// acquired here (e.g. an EF Core type, or a Website reference) would ripple everywhere.
/// </summary>
public class ContractArchitectureTests
{
    private static readonly Assembly ContractAssembly = typeof(IAccountRepository).Assembly;

    /// <summary>Contract should not depend on any implementation layer.</summary>
    [Theory]
    [InlineData("Maroik.Core.Repository")]
    [InlineData("Maroik.Core.Client")]
    [InlineData("Maroik.Core.Service")]
    [InlineData("Maroik.Core.PostgreSQL")]
    [InlineData("Maroik.Website")]
    [InlineData("Maroik.Worker")]
    [InlineData("Maroik.FileStorage")]
    public void Contract_ShouldNot_DependOnAnyImplementationLayer(string forbiddenNamespace)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ContractAssembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Maroik.Core.Contract must not reference {forbiddenNamespace}. " +
            "Contract is the shared abstraction layer every other layer depends on — it must have " +
            "zero dependency on any concrete implementation.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
