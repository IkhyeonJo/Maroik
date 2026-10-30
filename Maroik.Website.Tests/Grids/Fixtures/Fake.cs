using Moq;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>
/// Test doubles for the grid tests, which came from NonFactors AspNetCore.Grid 8.0.0 written against NSubstitute. A fake made
/// here behaves the way those tests expect of an NSubstitute substitute: a property keeps the value assigned to it, and a member
/// that returns an interface or abstract class returns a fake of it (so <c>HttpContext.RequestServices.GetService(...)</c> can be
/// set up in one expression). Configure it with <c>Mock.Get(fake).Setup(...)</c>.
/// </summary>
public static class Fake
{
    /// <summary>A loose Moq double of <typeparamref name="T"/> with stub properties and recursive fakes.</summary>
    public static T Of<T>() where T : class
    {
        Mock<T> mock = new() { DefaultValue = DefaultValue.Mock };
        mock.SetupAllProperties();

        return mock.Object;
    }
}
