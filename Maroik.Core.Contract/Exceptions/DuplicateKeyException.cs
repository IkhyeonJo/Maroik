using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Exceptions;

/// <summary>
/// A write lost to a uniqueness rule the services answer themselves, usually because a concurrent request got there first
/// after both passed the read-then-write check. It is an exception, not a repository return value, because a write inside a
/// transaction only reaches the database at <see cref="Interfaces.IUnitOfWork.CommitAsync"/>, where no repository call is
/// left to return anything. The repositories raise it; any other uniqueness failure stays the database's own exception.
/// </summary>
/// <param name="key">What was already taken.</param>
/// <param name="innerException">The database failure this reports.</param>
public sealed class DuplicateKeyException(DuplicateKey key, Exception? innerException = null)
    : Exception($"The value of {key} is already taken.", innerException)
{
    /// <summary>What was already taken.</summary>
    public DuplicateKey Key { get; } = key;
}
