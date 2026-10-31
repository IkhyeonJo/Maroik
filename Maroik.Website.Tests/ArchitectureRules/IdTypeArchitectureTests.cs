using System.Reflection;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Filters;

namespace Maroik.Website.Tests.ArchitectureRules;

/// <summary>
/// Every key column in <c>Init.sql</c> (each table's <c>Id</c> and every <c>*Id</c> foreign key) is <c>bigint</c>, so the HTTP
/// surface and the service contracts carry ids as <c>long</c> too. An <c>int</c> id (a view model property, an action
/// parameter, a service method parameter) cannot hold a row beyond <see cref="int.MaxValue"/>: model binding fails and the row
/// is reported missing, and narrowing a <c>long</c> back with <c>(int)</c> overflows silently.
/// </summary>
public class IdTypeArchitectureTests
{
    /// <summary>The Website assembly (controllers, view models, mappers, extensions).</summary>
    private static readonly Assembly _websiteAssembly = typeof(ViewBagPopulatorFilter).Assembly;
    /// <summary>The Contract assembly (service / repository interfaces and DTOs).</summary>
    private static readonly Assembly _contractAssembly = typeof(IMenuService).Assembly;

    /// <summary>Members declared on a type, of every visibility, static or not.</summary>
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>No property or method parameter named <c>id</c> / <c>*Id</c> in the Website or Contract is an <c>int</c>.</summary>
    [Fact]
    public void Ids_AreLong_LikeTheirBigintColumns()
    {
        string[] intIds =
        [
            .. new[] { _websiteAssembly, _contractAssembly }
                .SelectMany(a => a.GetTypes())
                .SelectMany(IntIdsOf)
                .Order(StringComparer.Ordinal)
        ];

        Assert.True(intIds.Length == 0, "These ids are int but their columns are bigint; make them long:\n" + string.Join("\n", intIds));
    }

    /// <summary>The <c>int</c> / <c>int?</c> ids <paramref name="type"/> declares, as "Type.Member" or "Type.Method(parameter)".</summary>
    private static IEnumerable<string> IntIdsOf(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(Declared))
        {
            if (IsId(property.Name) && IsInt(property.PropertyType))
                yield return $"{type.FullName}.{property.Name}";
        }

        foreach (MethodBase method in type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared)))
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.Name is { } name && IsId(name) && IsInt(parameter.ParameterType))
                    yield return $"{type.FullName}.{method.Name}({name})";
            }
        }
    }

    /// <summary>True for <c>id</c> / <c>Id</c> and for a name ending in <c>Id</c> (<c>boardId</c>, <c>CategoryId</c>).</summary>
    private static bool IsId(string name) =>
        name.Equals("id", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Id", StringComparison.Ordinal);

    /// <summary>True for <c>int</c> and <c>int?</c>.</summary>
    private static bool IsInt(Type type) => (Nullable.GetUnderlyingType(type) ?? type) == typeof(int);
}
