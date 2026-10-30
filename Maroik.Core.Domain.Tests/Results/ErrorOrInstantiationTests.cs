namespace Maroik.Core.Domain.Tests.Results;

using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Results.Interfaces;


public class ErrorOrInstantiationTests
{
    private record Person(string Name);

    [Fact]
    public void CreateFromValue_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        IEnumerable<string> value = ["value"];

        // Act
        ErrorOr<IEnumerable<string>> errorOrPerson = ErrorOrFactory.From(value);

        // Assert
        Assert.False(errorOrPerson.IsError);
        Assert.Same(value, errorOrPerson.Value);
    }

    [Fact]
    public void CreateFromValue_WhenAccessingValue_ViaStronglyTypedInterface_ShouldReturnValue()
    {
        // Arrange
        IEnumerable<string> value = ["value"];

        // Act
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        IErrorOr<IEnumerable<string>> errorOrValue = ErrorOrFactory.From(value);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

        // Assert
        Assert.False(errorOrValue.IsError);
        Assert.Same(value, errorOrValue.Value);
    }

    [Fact]
    public void CreateFromValue_WhenAccessingErrors_ShouldReturnUnexpectedError()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        ErrorOr<IEnumerable<string>> errorOrPerson = ErrorOrFactory.From(value);

        // Act
        List<Error> errors = errorOrPerson.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void CreateFromValue_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        ErrorOr<IEnumerable<string>> errorOrPerson = ErrorOrFactory.From(value);

        // Act
        List<Error> errors = errorOrPerson.ErrorsOrEmptyList;

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateFromValue_WhenAccessingFirstError_ShouldReturnUnexpectedError()
    {
        // Arrange
        IEnumerable<string> value = ["value"];
        ErrorOr<IEnumerable<string>> errorOrPerson = ErrorOrFactory.From(value);

        // Act
        Error firstError = errorOrPerson.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public async Task CreateFromAsyncValue_WhenAccessingValue_ShouldReturnValue()
    {
        // Arrange
        IEnumerable<string> value = ["value"];

        // Act
        ErrorOr<IEnumerable<string>> errorOrPerson = await ErrorOrFactory.FromAsync(value);

        // Assert
        Assert.False(errorOrPerson.IsError);
        Assert.Same(value, errorOrPerson.Value);
    }

    [Fact]
    public void CreateFromErrorList_UsingFactory_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        var error = Error.Validation("User.Name", "Name is too short");

        // Act
        ErrorOr<Person> errorOrPerson = ErrorOrFactory.From<Person>([error]);

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(error, Assert.Single(errorOrPerson.Errors));
    }

    [Fact]
    public async Task CreateFromAsyncErrorList_UsingFactory_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        var error = Error.Validation("User.Name", "Name is too short");

        // Act
        ErrorOr<Person> errorOrPerson = await ErrorOrFactory.FromAsync<Person>([error]);

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(error, Assert.Single(errorOrPerson.Errors));
    }

    [Fact]
    [Obsolete]
    public void CreateFromErrorList_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors = [Error.Validation("User.Name", "Name is too short")];
        ErrorOr<Person> errorOrPerson = ErrorOr<Person>.From(errors);

        // Act & Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors.Single(), Assert.Single(errorOrPerson.Errors));
    }

    [Fact]
    public void CreateFromErrorList_UsingFactory_WhenAccessingErrorsOrEmptyList_ShouldReturnErrorList()
    {
        // Arrange
        var error = Error.Validation("User.Name", "Name is too short");

        // Act
        ErrorOr<Person> errorOrPerson = ErrorOrFactory.From<Person>([error]);

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(error, Assert.Single(errorOrPerson.ErrorsOrEmptyList));
    }

    [Fact]
    [Obsolete]
    public void CreateFromErrorList_WhenAccessingErrorsOrEmptyList_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors = [Error.Validation("User.Name", "Name is too short")];
        ErrorOr<Person> errorOrPerson = ErrorOr<Person>.From(errors);

        // Act & Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors.Single(), Assert.Single(errorOrPerson.ErrorsOrEmptyList));
    }

    [Fact]
    public void CreateFromErrorList_UsingFactory_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = ErrorOrFactory.From<Person>([Error.Validation("User.Name", "Name is too short")]);

        // Act
        Person value = errorOrPerson.Value;

        // Assert
        Assert.Equal(default, value);
    }

    [Fact]
    [Obsolete]
    public void CreateFromErrorList_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        List<Error> errors = [Error.Validation("User.Name", "Name is too short")];
        ErrorOr<Person> errorOrPerson = ErrorOr<Person>.From(errors);

        // Act
        Person value = errorOrPerson.Value;

        // Assert
        Assert.Equal(default, value);
    }

    [Fact]
    public void CreateFromSingleError_UsingFactory_ShouldBeError()
    {
        // Act
        ErrorOr<Person> errorOrPerson = ErrorOrFactory.From<Person>(Error.Validation("User.Name", "Name is too short"));

        // Assert
        Assert.True(errorOrPerson.IsError);
    }

    [Fact]
    public async Task CreateFromAsyncSingleError_UsingFactory_ShouldBeError()
    {
        // Act
        ErrorOr<Person> errorOrPerson = await ErrorOrFactory.FromAsync<Person>(Error.Validation("User.Name", "Name is too short"));

        // Assert
        Assert.True(errorOrPerson.IsError);
    }

    [Fact]
    public void CreateFromArrayOfErrors_UsingFactory_ShouldBeError()
    {
        // Arrange
        Error[] errors = [
            Error.Validation("User.Name", "Name is too short"),
            Error.Forbidden("User.Forbidden", "You are not allowed to create user")
        ];

        // Act
        ErrorOr<Person> errorOrPerson = ErrorOrFactory.From<Person>(errors);

        // Assert
        Assert.True(errorOrPerson.IsError);
    }

    [Fact]
    public async Task CreateFromAsyncArrayOfErrors_UsingFactory_ShouldBeError()
    {
        // Arrange
        Error[] errors = [
            Error.Validation("User.Name", "Name is too short"),
            Error.Forbidden("User.Forbidden", "You are not allowed to create user")
        ];

        // Act
        ErrorOr<Person> errorOrPerson = await ErrorOrFactory.FromAsync<Person>(errors);

        // Assert
        Assert.True(errorOrPerson.IsError);
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingResult_ShouldReturnValue()
    {
        // Arrange
        Person result = new Person("Amici");

        // Act
        ErrorOr<Person> errorOr = result;

        // Assert
        Assert.False(errorOr.IsError);
        Assert.Equal(result, errorOr.Value);
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingErrors_ShouldReturnUnexpectedError()
    {
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        List<Error> errors = errorOrPerson.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastResult_WhenAccessingFirstError_ShouldReturnUnexpectedError()
    {
        ErrorOr<Person> errorOrPerson = new Person("Amichai");

        // Act
        Error firstError = errorOrPerson.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastPrimitiveResult_WhenAccessingResult_ShouldReturnValue()
    {
        // Arrange
        const int result = 4;

        // Act
        ErrorOr<int> errorOrInt = result;

        // Assert
        Assert.False(errorOrInt.IsError);
        Assert.Equal(result, errorOrInt.Value);
    }

    [Fact]
    public async Task ImplicitCastConditionalToObject_WhenBranchesAreDifferentErrorOrTypes_ShouldNestErrorOrValue()
    {
        // Repro guard for issue #144: conditional branch assignment can wrap ErrorOr<T> into ErrorOr<object>.
        var useFirstBranch = true;

        ErrorOr<object> request = useFirstBranch
            ? await CreateRequestModel1Async()
            : await CreateRequestModel2Async();

        Assert.False(request.IsError);
        Assert.IsType<ErrorOr<ModelClass1>>(request.Value);

        return;

        static Task<ErrorOr<ModelClass1>> CreateRequestModel1Async()
        {
            return Task.FromResult<ErrorOr<ModelClass1>>(new ModelClass1("first"));
        }

        static Task<ErrorOr<ModelClass2>> CreateRequestModel2Async()
        {
            return Task.FromResult<ErrorOr<ModelClass2>>(new ModelClass2(7));
        }
    }

    [Fact]
    public void ImplicitCastErrorOrType_WhenAccessingResult_ShouldReturnValue()
    {
        // Act
        ErrorOr<Success> errorOrSuccess = Result.Success;
        ErrorOr<Created> errorOrCreated = Result.Created;
        ErrorOr<Deleted> errorOrDeleted = Result.Deleted;
        ErrorOr<Updated> errorOrUpdated = Result.Updated;

        // Assert
        Assert.False(errorOrSuccess.IsError);
        Assert.Equal(Result.Success, errorOrSuccess.Value);

        Assert.False(errorOrCreated.IsError);
        Assert.Equal(Result.Created, errorOrCreated.Value);

        Assert.False(errorOrDeleted.IsError);
        Assert.Equal(Result.Deleted, errorOrDeleted.Value);

        Assert.False(errorOrUpdated.IsError);
        Assert.Equal(Result.Updated, errorOrUpdated.Value);
    }

    private record ModelClass1(string Name);

    private record ModelClass2(int Count);

    [Fact]
    public void ImplicitCastSingleError_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        Error error = Error.Validation("User.Name", "Name is too short");

        // Act
        ErrorOr<Person> errorOrPerson = error;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(error, Assert.Single(errorOrPerson.Errors));
    }

    [Fact]
    public void ImplicitCastSingleError_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<Person> errorOrPerson = Error.Validation("User.Name", "Name is too short");

        // Act
        Person value = errorOrPerson.Value;

        // Assert
        Assert.Equal(default, value);
    }

    [Fact]
    public void ImplicitCastSingleError_WhenAccessingFirstError_ShouldReturnError()
    {
        // Arrange
        Error error = Error.Validation("User.Name", "Name is too short");

        // Act
        ErrorOr<Person> errorOrPerson = error;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(error, errorOrPerson.FirstError);
    }

    [Fact]
    public void ImplicitCastErrorList_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        List<Error> errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        ErrorOr<Person> errorOrPerson = errors;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors.Count, errorOrPerson.Errors.Count);
        Assert.Equivalent(errors, errorOrPerson.Errors);
    }

    [Fact]
    public void ImplicitCastErrorArray_WhenAccessingErrors_ShouldReturnErrorArray()
    {
        // Arrange
        Error[] errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        ErrorOr<Person> errorOrPerson = errors;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors.Length, errorOrPerson.Errors.Count);
        Assert.Equivalent(errors, errorOrPerson.Errors);
    }

    [Fact]
    public void ImplicitCastErrorList_WhenAccessingFirstError_ShouldReturnFirstError()
    {
        // Arrange
        List<Error> errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        ErrorOr<Person> errorOrPerson = errors;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors[0], errorOrPerson.FirstError);
    }

    [Fact]
    public void ImplicitCastErrorArray_WhenAccessingFirstError_ShouldReturnFirstError()
    {
        // Arrange
        Error[] errors =
        [
            Error.Validation("User.Name", "Name is too short"),
            Error.Validation("User.Age", "User is too young"),
        ];

        // Act
        ErrorOr<Person> errorOrPerson = errors;

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(errors[0], errorOrPerson.FirstError);
    }

#pragma warning disable SA1129 // Do not use default value type constructor
    [Fact]
    public void CreateWithEmptyConstructor_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new ErrorOr<int>();

        // Act &Assert
        Assert.False(errorOrInt.IsError);
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void CreateWithEmptyConstructor_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new ErrorOr<int>();

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void CreateWithEmptyConstructor_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new ErrorOr<int>();

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateWithEmptyConstructor_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new ErrorOr<int>();

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }
#pragma warning restore SA1129 // Do not use default value type constructor

    [Fact]
    public void CreateWithDefault_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default;

        // Act & Assert
        Assert.False(errorOrInt.IsError);
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void CreateWithDefault_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default;

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void CreateWithDefault_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default;

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ErrorOr_FromErrorCollectionExpression_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        var nameTooShort = Error.Validation("User.Name", "Name is too short");
        var userTooYoung = Error.Validation("User.Age", "User is too young");

        // Act
        ErrorOr<Person> errorOrPerson = [nameTooShort, userTooYoung];

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.Equal(2, errorOrPerson.Errors.Count);
        Assert.Equivalent(new List<Error> { nameTooShort, userTooYoung }, errorOrPerson.Errors);
    }

    [Fact]
    public void ErrorOr_FromEmptyErrorCollectionExpression_ShouldBeError()
    {
        // Act
        ErrorOr<Person> errorOrPerson = [];

        // Assert
        Assert.True(errorOrPerson.IsError);
    }

    [Fact]
    public void GenericErrorOrInterface_FromErrorCollectionExpression_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        var nameTooShort = Error.Validation("User.Name", "Name is too short");
        var userTooYoung = Error.Validation("User.Age", "User is too young");

        // Act
        IErrorOr<Person> errorOrPerson = [nameTooShort, userTooYoung];

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.NotNull(errorOrPerson.Errors);
        Assert.Equal(2, errorOrPerson.Errors.Count);
        Assert.Equivalent(new List<Error> { nameTooShort, userTooYoung }, errorOrPerson.Errors);
    }

    [Fact]
    public void ErrorOrInterface_FromErrorCollectionExpression_WhenAccessingErrors_ShouldReturnErrorList()
    {
        // Arrange
        var nameTooShort = Error.Validation("User.Name", "Name is too short");
        var userTooYoung = Error.Validation("User.Age", "User is too young");

        // Act
        IErrorOr errorOrPerson = [nameTooShort, userTooYoung];

        // Assert
        Assert.True(errorOrPerson.IsError);
        Assert.NotNull(errorOrPerson.Errors);
        Assert.Equal(2, errorOrPerson.Errors.Count);
        Assert.Equivalent(new List<Error> { nameTooShort, userTooYoung }, errorOrPerson.Errors);
    }

    [Fact]
    public void CreateWithDefault_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default;

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorList_ShouldBeError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>();

        // Act & Assert
        Assert.True(errorOrInt.IsError);
    }

    [Fact]
    public void ImplicitCastEmptyErrorList_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>();

        // Act & Assert
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void ImplicitCastEmptyErrorList_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>();

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorList_WhenAccessingErrorsOrEmptyList_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>();

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorList_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = new List<Error>();

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorArray_ShouldBeError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Array.Empty<Error>();

        // Act & Assert
        Assert.True(errorOrInt.IsError);
    }

    [Fact]
    public void ImplicitCastEmptyErrorArray_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Array.Empty<Error>();

        // Act & Assert
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void ImplicitCastEmptyErrorArray_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Array.Empty<Error>();

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorArray_WhenAccessingErrorsOrEmptyList_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Array.Empty<Error>();

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastEmptyErrorArray_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = Array.Empty<Error>();

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastNullValue_WhenAccessingValue_ShouldReturnNull()
    {
        // Arrange
        ErrorOr<int?> errorOrInt = default(int?);

        // Act & Assert
        Assert.False(errorOrInt.IsError);
        Assert.Null(errorOrInt.Value);
    }

    [Fact]
    public void ImplicitCastNullObject_WhenAccessingValue_ShouldReturnNull()
    {
        // Arrange
        ErrorOr<Person?> result = CreatePersonOrNull(
            firstName: null,
            lastName: null);

        // Act & Assert
        Assert.False(result.IsError);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ImplicitCastNullValue_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int?> errorOrInt = default(int?);

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastNullValue_WhenAccessingErrorsOrEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        ErrorOr<int?> errorOrInt = default(int?);

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ImplicitCastNullValue_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int?> errorOrInt = default(int?);

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastNullErrorList_ShouldBeError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(List<Error>)!;

        // Act & Assert
        Assert.True(errorOrInt.IsError);
    }

    [Fact]
    public void ImplicitCastNullErrorList_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(List<Error>)!;

        // Act & Assert
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void ImplicitCastNullErrorList_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(List<Error>)!;

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastNullErrorList_WhenAccessingErrorsOrEmptyList_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(List<Error>)!;

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastNullErrorList_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(List<Error>)!;

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    [Fact]
    public void ImplicitCastNullErrorArray_ShouldBeError()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(Error[])!;

        // Act & Assert
        Assert.True(errorOrInt.IsError);
    }

    [Fact]
    public void ImplicitCastNullErrorArray_WhenAccessingValue_ShouldReturnDefault()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(Error[])!;

        // Act & Assert
        Assert.Equal(default, errorOrInt.Value);
    }

    [Fact]
    public void ImplicitCastNullErrorArray_WhenAccessingErrors_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(Error[])!;

        // Act
        List<Error> errors = errorOrInt.Errors;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastNullErrorArray_WhenAccessingErrorsOrEmptyList_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(Error[])!;

        // Act
        List<Error> errors = errorOrInt.ErrorsOrEmptyList;

        // Assert
        Assert.Equal(ErrorType.Unexpected, Assert.Single(errors).Type);
    }

    [Fact]
    public void ImplicitCastNullErrorArray_WhenAccessingFirstError_ShouldReturnUnexpected()
    {
        // Arrange
        ErrorOr<int> errorOrInt = default(Error[])!;

        // Act
        Error firstError = errorOrInt.FirstError;

        // Assert
        Assert.Equal(ErrorType.Unexpected, firstError.Type);
    }

    private static ErrorOr<Person?> CreatePersonOrNull(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            if (string.IsNullOrWhiteSpace(lastName))
            {
                return (Person?)null;
            }
            else
            {
                return Error.NotFound("Person.FirstNameNotFound", "First name is required.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(lastName))
            {
                return Error.NotFound("Person.LastNameNotFound", "Last name is required.");
            }
            else
            {
                return new Person($"{firstName} {lastName}");
            }
        }
    }
}
