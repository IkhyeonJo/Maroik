using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;
using Maroik.Core.Domain.Tests.Results.Fixtures;

namespace Maroik.Core.Domain.Tests.Results;

public class ThenAsyncTests
{
    [Fact]
    public async Task CallingThenAsync_WhenIsSuccess_ShouldInvokeNextThen()
    {
        // Arrange
        ErrorOr<string> errorOrString = "5";

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(num => Task.FromResult(num * 2))
            .ThenDoAsync(num => Task.Run(() => { _ = 5; }))
            .ThenAsync(ErrorOrConversions.ToStringAsync);

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.IsSuccess);
        Assert.Equivalent("10", result.Value);
    }

    [Fact]
    public async Task CallingThenAsync_WhenIsError_ShouldReturnErrors()
    {
        // Arrange
        ErrorOr<string> errorOrString = Error.NotFound();

        // Act
        ErrorOr<string> result = await errorOrString
            .ThenAsync(ErrorOrConversions.ToIntAsync)
            .ThenAsync(num => Task.FromResult(num * 2))
            .ThenDoAsync(num => Task.Run(() => { _ = 5; }))
            .ThenAsync(ErrorOrConversions.ToStringAsync);

        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equivalent(errorOrString.FirstError, result.FirstError);
    }
}
