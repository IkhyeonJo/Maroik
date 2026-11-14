using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Tests.Dtos;

/// <summary>
/// Unit tests for <see cref="ServiceValueResult{T}"/> — the result of a use case that hands back a value: a success always
/// carries the value, a failure carries the refusal's key, arguments, code and category and no value.
/// </summary>
public class ServiceValueResultTests
{
    /// <summary>A value converts to a success that carries it, with no error metadata.</summary>
    [Fact]
    public void AValue_ConvertsTo_ASuccessCarryingIt()
    {
        ServiceValueResult<string> result = "sheet";

        Assert.True(result.Success);
        Assert.Equal("sheet", result.Value);
        Assert.Equal("", result.ErrorKey);
        Assert.Empty(result.ErrorArgs);
        Assert.Equal("", result.ErrorCode);
        Assert.Equal(ServiceErrorType.None, result.ErrorType);
    }

    /// <summary>A failure converts to a failed result with the same key, arguments, code and category, and no value.</summary>
    [Fact]
    public void AFailure_ConvertsTo_AFailedResultWithItsErrorAndNoValue()
    {
        ServiceValueResult<string> result = ServiceResult.Conflict("X.Taken", "'{0}' is taken.", "name");

        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal("'{0}' is taken.", result.ErrorKey);
        Assert.Equal(["name"], result.ErrorArgs);
        Assert.Equal("X.Taken", result.ErrorCode);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    /// <summary>A failed result of a value type holds the type's default, not a stale number.</summary>
    [Fact]
    public void AFailure_OfAValueType_HoldsTheDefault()
    {
        ServiceValueResult<long> result = ServiceResult.Fail("Input is invalid");

        Assert.False(result.Success);
        Assert.Equal(0, result.Value);
    }

    /// <summary>A success without a value cannot become a result that promises one: the conversion is refused.</summary>
    [Fact]
    public void ASuccessWithoutAValue_CannotConvert()
    {
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
        {
            ServiceValueResult<string> _ = ServiceResult.Ok();
        });

        Assert.Equal("A successful ServiceValueResult<String> needs a value.", thrown.Message);
    }

    /// <summary>A failed result hands its failure on without the value, so it can become the failure of another result type.</summary>
    [Fact]
    public void ToResult_OfAFailure_IsTheFailureItCameFrom()
    {
        ServiceResult failure = ServiceResult.NotFound("X.NotFound", "not here");
        ServiceValueResult<string> result = failure;

        ServiceValueResult<long> other = result.ToResult();

        Assert.Same(failure, result.ToResult());
        Assert.False(other.Success);
        Assert.Equal("X.NotFound", other.ErrorCode);
    }

    /// <summary>A success without its value is still a success.</summary>
    [Fact]
    public void ToResult_OfASuccess_IsASuccess()
    {
        ServiceValueResult<string> result = "sheet";

        Assert.True(result.ToResult().Success);
    }
}
