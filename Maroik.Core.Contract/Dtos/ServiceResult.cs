namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Generic operation result used by service methods that do not need to return data.
/// Indicates success or failure and carries a localization error key on failure.
/// Use the static factory methods <see cref="Ok"/> and <see cref="Fail"/> to create instances.
/// </summary>
public class ServiceResult
{
    /// <summary>True when the operation completed successfully.</summary>
    public bool Success { get; private init; }

    /// <summary>Localization key that describes the error (empty string on success).</summary>
    public string ErrorKey { get; private init; } = "";

    /// <summary>Creates a successful result.</summary>
    public static ServiceResult Ok() => new() { Success = true };

    /// <summary>Creates a failed result with the given error key.</summary>
    public static ServiceResult Fail(string errorKey) => new() { Success = false, ErrorKey = errorKey };
}
