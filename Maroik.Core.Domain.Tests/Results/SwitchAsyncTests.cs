using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Extensions;

namespace Maroik.Core.Domain.Tests.Results;

public class SwitchAsyncTests
{
    private record Person(string Name);

    [Fact]
    public async Task CallingSwitchAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.CompletedTask;
        }
        Task ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = async () => await errorOrPerson.SwitchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchAsync_WhenIsError_ShouldExecuteElseAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task ThenAction(Person _) => throw new Exception("Should not be called");
        Task ElsesAction(IReadOnlyList<Error> errors)
        {
            Assert.Equivalent(errorOrPerson.Errors, errors);
            return Task.CompletedTask;
        }

        // Act
        Func<Task> action = async () => await errorOrPerson.SwitchAsync(
            ThenAction,
            ElsesAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchFirstAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.CompletedTask;
        }
        Task OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = async () => await errorOrPerson.SwitchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchFirstAsync_WhenIsError_ShouldExecuteOnFirstErrorAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new List<Error> { Error.Validation(), Error.Conflict() };
        Task ThenAction(Person _) => throw new Exception("Should not be called");
        Task OnFirstErrorAction(Error errors)
        {
            Assert.Equivalent(errorOrPerson.Errors[0], errors);
            Assert.Equivalent(errorOrPerson.FirstError, errors);
            return Task.CompletedTask;
        }

        // Act
        Func<Task> action = async () => await errorOrPerson.SwitchFirstAsync(
            ThenAction,
            OnFirstErrorAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchFirstAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.CompletedTask;
        }
        Task OnFirstErrorAction(Error _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = async () => await errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .SwitchFirstAsync(
                ThenAction,
                OnFirstErrorAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }

    [Fact]
    public async Task CallingSwitchAsyncAfterThenAsync_WhenIsSuccess_ShouldExecuteThenAction()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = new Person("Amichai");
        Task ThenAction(Person person)
        {
            Assert.Equivalent(errorOrPerson.Value, person);
            return Task.CompletedTask;
        }
        Task ElsesAction(IReadOnlyList<Error> _) => throw new Exception("Should not be called");

        // Act
        Func<Task> action = async () => await errorOrPerson
            .ThenAsync(person => Task.FromResult(person))
            .SwitchAsync(ThenAction, ElsesAction);

        // Assert
        Assert.Null(await Record.ExceptionAsync(action));
    }
}
