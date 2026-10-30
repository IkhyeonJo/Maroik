using System.Runtime.CompilerServices;

namespace Maroik.Core.Domain.Results.Interfaces;

[CollectionBuilder(typeof(CollectionExpression), nameof(CollectionExpression.CreateIErrorOrValue))]
public interface IErrorOr<out TValue> : IErrorOr
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    TValue Value { get; }
}
