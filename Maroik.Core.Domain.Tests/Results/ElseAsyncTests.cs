using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ElseAsyncTests
{
    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult($"Error count: {errors.Count}"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("Error count: 1", result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsError_ShouldInvokeElseFunc_ButWithErrorOrReturn()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetNewValue() => "OK";

        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(GetNewValue()));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("OK", result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorOrValueFunc_WhenIsSuccess_ShouldNotInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";
        bool elseFuncCalled = false;

        // Act
        ErrorOr<string> GetNewValue()
        {
            elseFuncCalled = true;
            return "OK";
        }

        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(GetNewValue()));

        // Assert
        Assert.False(elseFuncCalled);
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorOrValueFunc_WhenIsError_ShouldInvokeElseFunc_AndReturnError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetErrorValue() => Error.Unexpected();

        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(GetErrorValue()));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseAsyncWithValueFunc_WhenIsError_ShouldInvokeElseFunc_ButWithErrorOrReturn_ContinueChaining()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> GetNewValue() => "OK";

        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(GetNewValue()))
            .ThenAsync(value => Task.FromResult(value + "!"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("OK!", result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsSuccess_ShouldNotReturnElseValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithValue_WhenIsError_ShouldInvokeElseFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(Task.FromResult("oh no"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equivalent("oh no", result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseAsyncWithError_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(Task.FromResult(Error.Unexpected()));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(Error.Unexpected()));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseError()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(Error.Unexpected()));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsError_ShouldReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(new List<Error> { Error.Unexpected() }));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unexpected, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingElseAsyncWithErrorFunc_WhenIsSuccess_ShouldNotReturnElseErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ElseAsync(errors => Task.FromResult(new List<Error> { Error.Unexpected() }));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(errorOrString.Value, result.Value);
    }
}
