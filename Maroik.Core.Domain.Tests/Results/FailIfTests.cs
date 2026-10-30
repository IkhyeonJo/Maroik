using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class FailIfTests
{
    [Fact]
    public void CallingFailIf_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .FailIf(num => num > 3, Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingFailIfExtensionMethod_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIf(num => num > 3, Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
    }

    [Fact]
    public void CallingFailIf_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .FailIf(num => num > 10, Error.Failure());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void CallingFailIf_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .FailIf(str => str == string.Empty, Error.Failure());

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .FailIf(num => num > 3, (num) => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("5 is greater than 3", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingFailIfExtensionMethodWithErrorBuilder_WhenFailsIf_ShouldReturnError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenAsync(num => Task.FromResult(num))
            .FailIf(num => num > 3, (num) => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Failure, result.FirstError.Type);
        Assert.Equal("5 is greater than 3", result.FirstError.Description);
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenDoesNotFailIf_ShouldReturnValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .FailIf(num => num > 10, (num) => Error.Failure(description: $"{num} is greater than 10"));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void CallingFailIfWithErrorBuilder_WhenIsError_ShouldNotInvokeFailIfFunc()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();

        // Act
        ErrorOr<int> result = errorOrInt
            .FailIf(num => num > 3, (num) => Error.Failure(description: $"{num} is greater than 3"));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
