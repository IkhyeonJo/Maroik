using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class MatchTests
{
    private record Person(string Name);

    [Fact]
    public void CallingMatch_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        string ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return "Nice";
        }

        string ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<string> action = () => errorOrPerson.Match(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Equal("Nice", action());
    }

    [Fact]
    public void CallingMatch_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        string ThenAction(Person _) => throw new Exception("Should not be called");

        string ElsesAction(IReadOnlyList<Error> errors)
        {
            Assert.Equivalent(errorOrPerson.Errors, errors);
            return "Nice";
        }

        // Act
        Func<string> action = () => errorOrPerson.Match(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Equal("Nice", action());
    }

    [Fact]
    public void CallingMatchFirst_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        string ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return "Nice";
        }

        string OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<string> action = () => errorOrPerson.MatchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", action());
    }

    [Fact]
    public void CallingMatchFirst_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        string ThenAction(Person _) => throw new Exception("Should not be called");
        string OnFirstErrorAction(Error errors)
        {
            Assert.Equivalent(errorOrPerson.Errors[0], errors);
            Assert.Equivalent(errorOrPerson.FirstError, errors);

            return "Nice";
        }

        // Act
        Func<string> action = () => errorOrPerson.MatchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", action());
    }

    [Fact]
    public async Task CallingMatchFirstAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        string ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return "Nice";
        }

        string OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<Task<string>> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .MatchFirst(ThenAction, OnFirstErrorAction);

        // Assert
        Assert.Equal("Nice", await action());
    }

    [Fact]
    public async Task CallingMatchAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        string ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return "Nice";
        }

        string ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<Task<string>> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .Match(ThenAction, ElsesAction);

        // Assert
        Assert.Equal("Nice", await action());
    }
}
