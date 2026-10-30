using System.Globalization;
using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ThenEnsureAsyncTests
{
    [Fact]
    public async Task CallingThenEnsureAsync_WhenEnsureSucceeds_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenEnsureAsync(num => Task.FromResult<ErrorOr<int>>(num > 3 ? num + 10 : Error.Validation()));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public async Task CallingThenEnsureAsync_WhenEnsureReturnsError_ShouldReturnEnsureErrors()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenEnsureAsync(num => Task.FromResult<ErrorOr<int>>(num > 3 ? Error.Validation(description: $"{num} is too big") : num));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("5 is too big", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingThenEnsureAsync_WhenIsError_ShouldNotInvokeEnsure()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();
        bool called = false;

        // Act
        ErrorOr<int> result = await errorOrInt
            .ThenEnsureAsync(num =>
            {
                called = true;
                return Task.FromResult<ErrorOr<int>>(num);
            });

        // Assert
        Assert.False(called);
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task CallingThenEnsureAsyncAfterThenAsync_WhenEnsureSucceeds_ShouldContinueChainWithOriginalValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";
        int valueFromThenDo = 0;

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .Then(num => num * 2)
            .ThenEnsureAsync(num => Task.FromResult<ErrorOr<int>>(num > 3 ? num + 100 : Error.Validation()))
            .ThenDoAsync(num =>
            {
                valueFromThenDo = num;
                return Task.CompletedTask;
            })
            .Then(ErrorOrConversions.ToString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("10", result.Value);
        Assert.Equal(10, valueFromThenDo);
    }

    [Fact]
    public async Task CallingThenEnsureAsyncAfterThenAsync_WhenEnsureReturnsError_ShouldNotInvokeLaterChain()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";
        bool thenDoCalled = false;
        bool thenCalled = false;

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .Then(num => num + 1)
            .ThenEnsureAsync(num => Task.FromResult<ErrorOr<int>>(num > 3 ? Error.Validation(description: $"{num} is too big") : num))
            .ThenDoAsync(_ =>
            {
                thenDoCalled = true;
                return Task.CompletedTask;
            })
            .Then(num =>
            {
                thenCalled = true;
                return num.ToString(CultureInfo.InvariantCulture);
            });

        // Assert
        Assert.False(thenDoCalled);
        Assert.False(thenCalled);
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("6 is too big", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingThenEnsureAsyncExtensionMethod_WhenEnsureSucceeds_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await Task.FromResult(errorOrInt)
            .ThenEnsureAsync(num => Task.FromResult<ErrorOr<int>>(num > 3 ? num + 100 : Error.Validation()));

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }
}
