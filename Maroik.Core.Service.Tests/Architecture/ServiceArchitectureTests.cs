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

    /// <summary>
    /// Repositories for entities that live only as children of an aggregate root
    /// (BoardComment/BoardAttachedFile under Board, CalendarEventReminder/CalendarEventAttachedFile/
    /// CalendarShared under Calendar) expose Create/UpdateEntityAsync directly on the child type via
    /// IGenericRepository, the same shape as a top-level aggregate repository. Nothing in the type
    /// system stops some other service from injecting one of these and persisting a child mutation
    /// that never went through the parent aggregate's own invariant-enforcing methods (Board.AddComment,
    /// etc.) — so this test is the guard rail instead: only the one service that owns the parent
    /// aggregate may reference each child repository's interface at all.
    /// </summary>
    [Theory]
    [InlineData("BoardService", "IBoardCommentRepository")]
    [InlineData("BoardService", "IBoardAttachedFileRepository")]
    [InlineData("CalendarService", "ICalendarEventReminderRepository")]
    [InlineData("CalendarService", "ICalendarEventAttachedFileRepository")]
    [InlineData("CalendarService", "ICalendarSharedRepository")]
    public void OnlyOwningAggregateService_ShouldDependOn_ChildEntityRepository(string owningServiceName, string childRepositoryInterfaceName)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(ServiceAssembly)
            .That()
            .DoNotHaveName(owningServiceName)
            .ShouldNot()
            .HaveDependencyOn(childRepositoryInterfaceName)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Only {owningServiceName} may depend on {childRepositoryInterfaceName} — it persists a " +
            "child entity that must only be mutated after going through its aggregate root's own " +
            "domain methods. Route the operation through the aggregate root's service instead.\n" +
            "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
