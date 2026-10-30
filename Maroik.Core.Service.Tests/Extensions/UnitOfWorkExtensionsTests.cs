using Maroik.Core.Domain.Results;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Tests.Fixtures;

namespace Maroik.Core.Service.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkExtensions"/> — the shared "roll back the current
/// transaction and return a failure" helper shared by
/// <c>IncomeService</c>, <c>ExpenditureService</c>, and <c>CalendarService</c>.
/// </summary>
public class UnitOfWorkExtensionsTests : IAsyncDisposable
{
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();

    // -- FailAsync(ServiceResult) -----------------------------------------------

    /// <summary>Rolls back the transaction and returns the given result unchanged.</summary>
    [Fact]
    public async Task FailAsync_WithServiceResult_RollsBackAndReturnsTheResult()
    {
        ServiceResult expected = ServiceResult.Conflict("Test.Conflict", "Already exists.");

        ServiceResult result = await _unitOfWork.FailAsync(expected, TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    // -- FailAsync(Error) --------------------------------------------------------

    /// <summary>Rolls back the transaction and maps the domain error into an equivalent failed <see cref="ServiceResult"/>.</summary>
    [Fact]
    public async Task FailAsync_WithError_RollsBackAndReturnsAMappedFailure()
    {
        Error error = Error.Conflict("Test.Conflict", "Already exists.");

        ServiceResult result = await _unitOfWork.FailAsync(error, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Test.Conflict", result.ErrorCode);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
