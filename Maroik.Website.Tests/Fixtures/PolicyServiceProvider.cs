using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>The services a view model's validation asks for, as MVC's request services would give them: the real policy service.</summary>
public sealed class PolicyServiceProvider : IServiceProvider
{
    /// <inheritdoc />
    public object? GetService(Type serviceType) => serviceType == typeof(IPolicyService) ? new PolicyService() : null;
}
