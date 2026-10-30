namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>A hand-written <see cref="IServiceProvider"/> that answers the services it was given by type, and <see langword="null"/> for any other.</summary>
public sealed class FakeServiceProvider(params (Type Type, Object Service)[] services) : IServiceProvider
{
    /// <summary>The services by type.</summary>
    private readonly Dictionary<Type, Object> _services = services.ToDictionary(s => s.Type, s => s.Service);

    /// <inheritdoc/>
    public Object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
}
