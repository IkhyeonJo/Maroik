using ErrorOr;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Extensions;

/// <summary>Shared transaction-rollback helper for services built around <see cref="IUnitOfWork"/>.</summary>
public static class UnitOfWorkExtensions
{
    /// <summary>Rolls back the current transaction on <paramref name="unitOfWork"/> and returns <paramref name="result"/>.</summary>
    public static async Task<ServiceResult> FailAsync(this IUnitOfWork unitOfWork, ServiceResult result, CancellationToken ct)
    {
        await unitOfWork.RollbackAsync(ct);
        return result;
    }

    /// <summary>Rolls back the current transaction on <paramref name="unitOfWork"/> and returns a failure mapped from <paramref name="error"/>.</summary>
    public static Task<ServiceResult> FailAsync(this IUnitOfWork unitOfWork, Error error, CancellationToken ct)
        => unitOfWork.FailAsync(ServiceResult.FromError(error), ct);
}
