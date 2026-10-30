using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ElseTests
{
    [Fact]
    public void CallingElseWithValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => $"Error count: {errors.Count}");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public void CallingElseWithValueFunc_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => $"Error count: {errors.Count}");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("Error count: 1", result.Value);
    }

    [Fact]
    public void CallingElseWithValue_WhenIsSuccess_ShouldNotReturnElseValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else("oh no");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public void CallingElseWithValue_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else("oh no");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("oh no", result.Value);
    }

    [Fact]
    public void CallingElseWithError_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(Error.Unexpected());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public void CallingElseWithError_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(Error.Unexpected());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }

    [Fact]
    public void CallingElseWithErrorsFunc_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => Error.Unexpected());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public void CallingElseWithErrorsFunc_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => Error.Unexpected());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }

    [Fact]
    public void CallingElseWithErrorsFunc_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => [Error.Unexpected()]);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public void CallingElseWithErrorsFunc_WhenIsSuccess_ShouldNotReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(ErrorOrConversions.ToString)
            .Else(errors => [Error.Unexpected()]);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseWithValueAfterThenAsync_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else("oh no");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("oh no", result.Value);
    }

    [Fact]
    public async Task CallingElseWithValueFuncAfterThenAsync_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => $"Error count: {errors.Count}");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("Error count: 1", result.Value);
    }

    [Fact]
    public async Task CallingElseWithValueFuncAfterThenAsync_WhenIsError_ShouldInvokeElseFunc_ButWithErrorOrReturn()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetNewValue() => "OK";

        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => GetNewValue());

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("OK", result.Value);
    }

    [Fact]
    public async Task CallingElseWithValueFuncAfterThenAsync_WhenIsError_ShouldInvokeElseFunc_ButWithErrorOrReturn_ContinueChaining()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetNewValue() => "OK";

        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => GetNewValue())
            .Then(value => value + "!");

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("OK!", result.Value);
    }

    [Fact]
    public async Task CallingElseWithErrorAfterThenAsync_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(Error.Unexpected());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseWithErrorFuncAfterThenAsync_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => Error.Unexpected());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseWithErrorFuncAfterThenAsync_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => [Error.Unexpected()]);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public void CallingElseWithErrorOrValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> GetNewValue() => "OK";
        ErrorOr<string> result = errorOrString
            .Else(errors => GetNewValue());

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public void CallingElseWithErrorOrValueFunc_WhenIsError_ShouldInvokeElseFunc_AndReturnError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetErrorValue() => Error.Unexpected();
        ErrorOr<string> result = errorOrString
            .Else(errors => GetErrorValue());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseWithErrorOrValueFunc_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetErrorValue() => Error.Unexpected();
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => GetErrorValue());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseWithErrorOrValueFunc_WhenIsError_ShouldReturnElseFuncValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetErrorValue() => "5";
        ErrorOr<string> result = await errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Else(errors => GetErrorValue());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("5", result.Value);
    }
}
