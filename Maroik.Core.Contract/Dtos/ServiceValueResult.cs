using System.Diagnostics.CodeAnalysis;

namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// The result of a service method that hands back a value: either a success that carries the value, or a failure that
/// carries what a failed <see cref="ServiceResult"/> carries and no value. One object instead of a
/// <c>(ServiceResult, T?)</c> pair, so "succeeded but no value" cannot be written and the caller needs no <c>!</c>:
/// after <c>if (!result.Success) return ...;</c> the compiler knows <see cref="Value"/> is there.
/// A service returns the value or the failure itself (<c>return folder;</c> / <c>return ItemNotFound();</c>); both convert.
/// </summary>
/// <typeparam name="T">The type of the value a success carries.</typeparam>
public sealed class ServiceValueResult<T>
{
    /// <summary>The success or the failure this result stands for.</summary>
    private readonly ServiceResult _outcome;

    /// <summary>Creates the result of <paramref name="outcome"/> with <paramref name="value"/>.</summary>
    private ServiceValueResult(ServiceResult outcome, T? value)
    {
        _outcome = outcome;
        Value = value;
    }

    /// <summary>True when the operation completed successfully; <see cref="Value"/> is then present.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool Success => _outcome.Success;

    /// <summary>The value of a success; the type's default (null, 0) on a failure.</summary>
    public T? Value { get; }

    /// <inheritdoc cref="ServiceResult.ErrorKey"/>
    public string ErrorKey => _outcome.ErrorKey;

    /// <inheritdoc cref="ServiceResult.ErrorArgs"/>
    public object[] ErrorArgs => _outcome.ErrorArgs;

    /// <inheritdoc cref="ServiceResult.ErrorCode"/>
    public string ErrorCode => _outcome.ErrorCode;

    /// <inheritdoc cref="ServiceResult.ErrorType"/>
    public ServiceErrorType ErrorType => _outcome.ErrorType;

    /// <summary>
    /// The outcome without the value: the failure this result was made from (so it can be returned as the failure of another
    /// result type), or a plain success.
    /// </summary>
    public ServiceResult ToResult() => _outcome;

    /// <summary>A success that carries <paramref name="value"/>.</summary>
    public static implicit operator ServiceValueResult<T>(T value) => new(ServiceResult.Ok(), value);

    /// <summary>
    /// The failure <paramref name="failure"/> as a result of this type. A success is refused: it has no value to carry,
    /// and a caller that saw <see cref="Success"/> would read a missing one.
    /// </summary>
    public static implicit operator ServiceValueResult<T>(ServiceResult failure) =>
        failure.Success
            ? throw new InvalidOperationException($"A successful ServiceValueResult<{typeof(T).Name}> needs a value.")
            : new(failure, default);
}
