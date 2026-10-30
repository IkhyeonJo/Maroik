using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class ToErrorOrTests
{
    [Fact]
    public void ValueToErrorOr_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        int value = 5;

        // Act
        ErrorOr<int> result = value.ToErrorOr();

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public async Task TaskToErrorOrAsync_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        int value = 5;
        Task<int> task = Task.FromResult(value);

        // Act
        ErrorOr<int> result = await task.ToErrorOrAsync();

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ErrorToErrorOr_WhenAccessingFirstError_ShouldReturnSameError()
    {
        // Arrange
        Error error = Error.Unauthorized();

        // Act
        ErrorOr<int> result = error.ToErrorOr<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(error, result.FirstError);
    }

    [Fact]
    public async Task ErrorToErrorOrAsync_WhenAccessingFirstError_ShouldReturnSameError()
    {
        // Arrange
        Error error = Error.Unauthorized();
        Task<Error> task = Task.FromResult(error);

        // Act
        ErrorOr<int> result = await task.ToErrorOrAsync<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(error, result.FirstError);
    }

    [Fact]
    public void ListOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSameErrors()
    {
        // Arrange
        List<Error> errors = [Error.Unauthorized(), Error.Validation()];

        // Act
        ErrorOr<int> result = errors.ToErrorOr<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equivalent(errors, result.Errors);
    }

    [Fact]
    public async Task ListOfErrorsToErrorOrAsync_WhenAccessingErrors_ShouldReturnSameErrors()
    {
        // Arrange
        List<Error> errors = [Error.Unauthorized(), Error.Validation()];
        Task<List<Error>> task = Task.FromResult(errors);

        // Act
        ErrorOr<int> result = await task.ToErrorOrAsync<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equivalent(errors, result.Errors);
    }

    [Fact]
    public void EnumerableOfErrorsToErrorOr_WhenAccessingErrors_ShouldReturnSimilarErrors()
    {
        // Arrange
        Error[] errors = [Error.Unauthorized(), Error.Validation()];

        // Act
        ErrorOr<int> result = errors.ToErrorOr<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public async Task ArrayOfErrorsToErrorOrAsync_WhenAccessingErrors_ShouldReturnSimilarErrors()
    {
        // Arrange
        Error[] errors = [Error.Unauthorized(), Error.Validation()];
        Task<Error[]> task = Task.FromResult(errors);

        // Act
        ErrorOr<int> result = await task.ToErrorOrAsync<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public async Task EnumerableOfErrorsToErrorOrAsync_WhenAccessingErrors_ShouldReturnSimilarErrors()
    {
        // Arrange
        Error[] errors = [Error.Unauthorized(), Error.Validation()];
        Task<IEnumerable<Error>> task = Task.FromResult((IEnumerable<Error>)errors);

        // Act
        ErrorOr<int> result = await task.ToErrorOrAsync<int>();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(errors, result.Errors);
    }
}
