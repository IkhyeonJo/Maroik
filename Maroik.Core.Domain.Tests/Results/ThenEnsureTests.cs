using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ThenEnsureTests
{
    [Fact]
    public void CallingThenEnsure_WhenEnsureSucceeds_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .ThenEnsure(num => num > 10 ? Error.Validation() : num + 10);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void CallingThenEnsure_WhenEnsureReturnsError_ShouldReturnEnsureErrors()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = errorOrInt
            .ThenEnsure(num => num > 3 ? Error.Validation(description: $"{num} is too big") : num);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("5 is too big", result.FirstError.Description);
    }

    [Fact]
    public void CallingThenEnsure_WhenIsError_ShouldNotInvokeEnsure()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Error.NotFound();
        bool called = false;

        // Act
        ErrorOr<int> result = errorOrInt
            .ThenEnsure(num =>
            {
                called = true;
                return num;
            });

        // Assert
        Assert.False(called);
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public void CallingThenEnsureAfterThen_WhenEnsureSucceeds_ShouldContinueChainWithOriginalValue()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";
        int valueFromThenDo = 0;

        // Act
        ErrorOr<string> result = errorOrString
            .Then(ErrorOrConversions.ToInt)
            .ThenEnsure(num => num > 3 ? num + 10 : Error.Validation())
            .ThenDo(num => valueFromThenDo = num)
            .Then(ErrorOrConversions.ToString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("5", result.Value);
        Assert.Equal(5, valueFromThenDo);
    }

    [Fact]
    public void CallingThenEnsureAfterThen_WhenEnsureReturnsError_ShouldNotInvokeLaterChain()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;
        bool thenDoCalled = false;
        bool thenCalled = false;

        // Act
        ErrorOr<int> result = errorOrInt
            .Then(num => num + 1)
            .ThenEnsure(num => num > 3 ? Error.Validation(description: $"{num} is too big") : num)
            .ThenDo(_ => thenDoCalled = true)
            .Then(num =>
            {
                thenCalled = true;
                return num * 2;
            });

        // Assert
        Assert.False(thenDoCalled);
        Assert.False(thenCalled);
        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("6 is too big", result.FirstError.Description);
    }

    [Fact]
    public async Task CallingThenEnsureExtensionMethod_WhenEnsureSucceeds_ShouldReturnOriginalValue()
    {
        // Arrange
        ErrorOr<int> errorOrInt = 5;

        // Act
        ErrorOr<int> result = await Task.FromResult(errorOrInt)
            .ThenEnsure(num => num > 3 ? num + 100 : Error.Validation());

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value);
    }
}
