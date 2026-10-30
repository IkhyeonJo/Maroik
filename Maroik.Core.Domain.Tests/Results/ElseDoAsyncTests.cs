using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ElseDoAsyncTests
{
    [Fact]
    public async Task CallingElseDoAsync_WhenIsError_ShouldInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.Validation();

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrString
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            });

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal(1, errorCounter);
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsSuccess_ShouldNotInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrString
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            });

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("5", result.Value);
        Assert.Equal(0, errorCounter);
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsError_ShouldInvokeNextElse()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.Validation();

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrString
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            })
            .Else(error => $"error count is {errorCounter}");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("error count is 1", result.Value);
        Assert.Equal(1, errorCounter);
    }

    [Fact]
    public async Task CallingElseDoAsync_WhenIsSuccess_ShouldInvokeNextElse()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrString
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            })
            .Else(error => $"is not called");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("5", result.Value);
        Assert.Equal(0, errorCounter);
    }

    [Fact]
    public async Task CallingElseDoAsync_AfterThenAsync_WhenIsError_ShouldInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.Validation();

        // Act
        int errorCounter = 0;
        ErrorOr<int> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            });

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(1, errorCounter);
    }

    [Fact]
    public async Task CallingElseDoAsync_AfterThenAsync_WhenIsSuccess_ShouldNotInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        int errorCounter = 0;
        ErrorOr<int> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ElseDoAsync(error =>
            {
                errorCounter += error.Count;
                return Task.CompletedTask;
            });

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
        Assert.Equal(0, errorCounter);
    }
}
