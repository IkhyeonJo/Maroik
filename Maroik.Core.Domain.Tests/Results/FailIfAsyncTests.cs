using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class FailIfAsyncTests
{
    private record Person(string Name);

    [Fact]
    public async Task CallingFailIfAsync_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethod_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIfAsync(num => Task.FromResult(num > 3), Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingFailIfAsync_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 10), Error.Failure());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public async Task CallingFailIf_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .FailIfAsync(str => Task.FromResult(str == string.Empty), Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("5 is greater than 3.", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingFailIfAsyncExtensionMethodWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("5 is greater than 3.", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingFailIfAsyncWithErrorBuilder_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 10), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 10.")));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public async Task CallingFailIfWithErrorBuilder_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();

        // Act
        ErrorOr<int> result = await errorOrInt
            .FailIfAsync(num => Task.FromResult(num > 3), (num) => Task.FromResult(Error.Failure(description: $"{num} is greater than 3.")));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
