using System.Reflection;
using Maroik.Worker.Workers;
using NetArchTest.Rules;

namespace Maroik.Worker.Tests.Architecture;

/// <summary>
/// Architecture test enforcing that Maroik.Worker only consumes the infrastructure
/// implementations (Maroik.Core.Client, Maroik.Core.Repository) via interfaces declared
/// in Maroik.Core.Contract. Direct dependencies on those implementation namespaces are
/// forbidden for all types except Program (the composition root), which wires up
/// implementations into the DI container — mirroring WebsiteArchitectureTests.
/// </summary>
public class WorkerArchitectureTests
{
    private static readonly Assembly WorkerAssembly = typeof(EmailConsumerWorker).Assembly;

    private const string RepositoryNamespace = "Maroik.Core.Repository";
    private const string ClientNamespace = "Maroik.Core.Client";
    private const string PostgreSqlNamespace = "Maroik.Core.PostgreSQL";

    /// <summary>Worker types except composition root should not depend on repository namespace.</summary>
    [Fact]
    public void WorkerTypes_ExceptCompositionRoot_ShouldNot_DependOnRepositoryNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WorkerAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(RepositoryNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Worker must not reference " +
            "Maroik.Core.Repository directly. " +
            "Use repository interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>Worker types except composition root should not depend on client namespace.</summary>
    [Fact]
    public void WorkerTypes_ExceptCompositionRoot_ShouldNot_DependOnClientNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WorkerAssembly)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOn(ClientNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Non-composition-root types in Maroik.Worker must not reference " +
            "Maroik.Core.Client directly. " +
            "Use client interfaces from Maroik.Core.Contract instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// No type in Maroik.Worker, including Program, should depend on Maroik.Core.PostgreSQL.
    /// Even the composition root only needs to register EF Core via
    /// <c>AddRepositoryContext</c> in Maroik.Core.Repository, so there is no legitimate
    /// reason for Worker to reference the PostgreSQL project at all.
    /// </summary>
    [Fact]
    public void WorkerTypes_ShouldNot_DependOnPostgreSqlNamespace()
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(WorkerAssembly)
            .That()
            .ResideInNamespaceStartingWith("Maroik.Worker")
            .ShouldNot()
            .HaveDependencyOn(PostgreSqlNamespace)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Maroik.Worker must not reference Maroik.Core.PostgreSQL directly, not even from " +
            "Program. Register EF Core via AddRepositoryContext in Maroik.Core.Repository instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
