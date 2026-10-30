using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ThenTests
{
    [Fact]
    public void CallingThen_WhenIsSuccess_ShouldInvokeGivenFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(num => num * 2)
            .Then(ErrorOrConversions.ToString);

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.IsSuccess);
        Assert.Equivalent("10", result.Value);
    }

    [Fact]
    public void CallingThen_WhenIsSuccess_ShouldInvokeGivenAction()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<int> result = errorOrString
            .ThenDo(str => { _ = 5; })
            .Then(ErrorOrConversions.ToInt)
            .ThenDo(str => { _ = 5; });

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void CallingThen_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .Then(num => num * 2)
            .ThenDo(str => { _ = 5; })
            .Then(ErrorOrConversions.ToString);

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equivalent(errorOrString.FirstError, result.FirstError);
    }

    [Fact]
    public async Task CallingThenAfterThenAsync_WhenIsSuccess_ShouldInvokeGivenFunc()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .Then(num => num * 2)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .Then(ErrorOrConversions.ToInt)
            .ThenAsync(ErrorOrConversions.ToStringAsync)
            .ThenDo(num => { _ = 5; });

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.IsSuccess);
        Assert.Equal("10", result.Value);
    }

    [Fact]
    public async Task CallingThenAfterThenAsync_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .Then(ErrorOrConversions.ToString);

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equivalent(errorOrString.FirstError, result.FirstError);
    }
}
