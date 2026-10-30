using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class MatchAsyncTests
{
    private record Person(string Name);

    [Fact]
    public async Task CallingMatchAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task<string> ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.FromResult("Nice");
        }

        Task<string> ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<Task<string>> action = async () => await errorOrPerson.MatchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchAsync_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task<string> ThenAction(Person _) => throw new Exception("Should not be called");

        Task<string> ElsesAction(IReadOnlyList<Error> errors)
        {
            Assert.Equivalent(errorOrPerson.Errors, errors);
            return Task.FromResult("Nice");
        }

        // Act
        Func<Task<string>> action = async () => await errorOrPerson.MatchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchFirstAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task<string> ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.FromResult("Nice");
        }

        Task<string> OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<Task<string>> action = async () => await errorOrPerson.MatchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchFirstAsync_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task<string> ThenAction(Person _) => throw new Exception("Should not be called");
        Task<string> OnFirstErrorAction(Error errors)
        {
            Assert.Equivalent(errorOrPerson.Errors[0], errors);
            Assert.Equivalent(errorOrPerson.FirstError, errors);

            return Task.FromResult("Nice");
        }

        // Act
        Func<Task<string>> action = async () => await errorOrPerson.MatchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchFirstAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task<string> ThenAction(Person _) => throw new Exception("Should not be called");
        Task<string> OnFirstErrorAction(Error errors)
        {
            Assert.Equivalent(errorOrPerson.Errors[0], errors);
            Assert.Equivalent(errorOrPerson.FirstError, errors);

            return Task.FromResult("Nice");
        }

        // Act
        Func<Task<string>> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .MatchFirstAsync(ThenAction, OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task<string> ThenAction(Person _) => throw new Exception("Should not be called");

        Task<string> ElsesAction(IReadOnlyList<Error> errors)
        {
            Assert.Equivalent(errorOrPerson.Errors, errors);
            return Task.FromResult("Nice");
        }

        // Act
        Func<Task<string>> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .MatchAsync(ThenAction, ElsesAction);

        // Assert
        Assert.Equal("Nice", await action());
    }
}
