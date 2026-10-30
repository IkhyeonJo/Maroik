using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ElseDoTests
{
    [Fact]
    public async Task CallingElseDo_WhenIsError_ShouldInvokeSyncAction()
    {
        // Arrange
        Task<ErrorOr<string>> errorOrTask = ErrorOrFactory.FromAsync<string>(Error.Validation());

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrTask
            .ElseDo(error => errorCounter += error.Count);

        // Assert
        Assert.Equal(1, errorCounter);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CallingElseDo_WhenIsSuccess_ShouldInvokeSyncAction()
    {
        // Arrange
        Task<ErrorOr<string>> errorOrTask = Task.FromResult(ErrorOrFactory.From<string>("5"));

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = await errorOrTask
            .ElseDo(error => errorCounter += error.Count);

        // Assert
        Assert.Equal(0, errorCounter);
        Assert.False(result.IsError);
        Assert.Equal("5", result.Value);
    }

    [Fact]
    public void CallingElseDo_WhenIsError_ShouldInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.Validation();

        // Act
        int errorCounter = 0;
        ErrorOr<int> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ElseDo(error => errorCounter += error.Count())
            .Then(_ => 10);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal(1, errorCounter);
    }

    [Fact]
    public void CallingElseDo_WhenIsSuccess_ShouldInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        int errorCounter = 0;
        ErrorOr<int> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ElseDo(error => errorCounter += error.Count())
            .Then(_ => 10);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(0, errorCounter);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void CallingElseDo_WhenIsSuccess_ShouldNotInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        int errorCounter = 0;
        ErrorOr<int> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ElseDo(error => errorCounter += error.Count());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
        Assert.Equal(0, errorCounter);
    }

    [Fact]
    public void CallingElseDo_WhenIsError_ShouldInvokeNextElse()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.Failure();

        // Act
        int errorCounter = 0;
        ErrorOr<string> result = errorOrString
            .ElseDo(error => errorCounter += error.Count())
            .Else(error => $"error count is {errorCounter}");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("error count is 1", result.Value);
        Assert.Equal(1, errorCounter);
    }
}
