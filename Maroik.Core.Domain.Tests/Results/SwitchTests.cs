using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class SwitchTests
{
    private record Person(string Name);

    [Fact]
    public void CallingSwitch_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => Assert.Equivalent(errorOrPerson.Value, person);
        void ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Action action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Null(Record.Exception(action));
    }

    [Fact]
    public void CallingSwitch_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        void ThenAction(Person _) => throw new Exception("Should not be called");
        void ElsesAction(IReadOnlyList<Error> errors) => Assert.Equivalent(errorOrPerson.Errors, errors);

        // Act
        Action action = () => errorOrPerson.Switch(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Null(Record.Exception(action));
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => Assert.Equivalent(errorOrPerson.Value, person);
        void OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Action action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Null(Record.Exception(action));
    }

    [Fact]
    public void CallingSwitchFirst_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        void ThenAction(Person _) => throw new Exception("Should not be called");
        void OnFirstErrorAction(Error errors)
        {
            Assert.Equivalent(errorOrPerson.Errors[0], errors);
            Assert.Equivalent(errorOrPerson.FirstError, errors);
        }

        // Act
        Action action = () => errorOrPerson.SwitchFirst(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Null(Record.Exception(action));
    }

    [Fact]
    public async Task CallingSwitchFirstAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => Assert.Equivalent(errorOrPerson.Value, person);
        void OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .SwitchFirst(ThenAction, OnFirstErrorAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        void ThenAction(Person person) => Assert.Equivalent(errorOrPerson.Value, person);
        void ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = () => errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .Switch(ThenAction, ElsesAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }
}
