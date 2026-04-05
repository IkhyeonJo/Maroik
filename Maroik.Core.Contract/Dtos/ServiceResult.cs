namespace Maroik.Core.Contract.Dtos;

public class ServiceResult
{
    public bool Success { get; init; }
    public string ErrorKey { get; init; } = string.Empty;

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string errorKey) => new() { Success = false, ErrorKey = errorKey };
}
