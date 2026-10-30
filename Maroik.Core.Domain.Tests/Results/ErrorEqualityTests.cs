using Maroik.Core.Domain.Results;

namespace Maroik.Core.Domain.Tests.Results;

public sealed class ErrorEqualityTests
{
    public static readonly TheoryData<string, string, int, Dictionary<string, object>?> ValidData =
        new ()
        {
            { "CodeA", "DescriptionA", 1, null },
            { "CodeB", "DescriptionB", 3215, new Dictionary<string, object> { { "foo", "bar" }, { "baz", "qux" } } },
        };

    public static readonly TheoryData<Error, Error> DifferentInstances =
        new ()
        {
            { Error.Failure(), Error.Forbidden() },
            { Error.NotFound(), Error.NotFound(metadata: new Dictionary<string, object> { ["Foo"] = "Bar" }) },
            { Error.Unexpected(metadata: new Dictionary<string, object> { ["baz"] = "qux" }), Error.Unexpected() },
            {
                Error.Failure(metadata: new Dictionary<string, object> { ["baz"] = "qux" }),
                Error.Failure(metadata: new Dictionary<string, object> { ["Foo"] = "Bar", ["baz"] = "qux" })
            },
            {
                Error.Failure(metadata: new Dictionary<string, object> { ["baz"] = "qux" }),
                Error.Failure(metadata: new Dictionary<string, object> { ["baz"] = "gorge" })
            },
        };

    [Theory]
    [MemberData(nameof(ValidData))]
    public void Equals_WhenTwoInstancesHaveTheSameValues_ShouldReturnTrue(
        string code,
        string description,
        int numericType,
        Dictionary<string, object>? metadata)
    {
        var error1 = Error.Custom(numericType, code, description, metadata);
        var clonedDictionary = metadata is null ? null : new Dictionary<string, object>(metadata);
        var error2 = Error.Custom(numericType, code, description, clonedDictionary);

        var result = error1.Equals(error2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_WhenTwoInstancesHaveTheSameMetadataInstanceAndPropertyValues_ShouldReturnTrue()
    {
        var metadata = new Dictionary<string, object> { { "foo", "bar" } };
        var error1 = Error.Custom(1, "Code", "Description", metadata);
        var error2 = Error.Custom(1, "Code", "Description", metadata);

        var result = error1.Equals(error2);

        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(DifferentInstances))]
    public void Equals_WhenTwoInstancesHaveDifferentValues_ShouldReturnFalse(Error error1, Error error2)
    {
        var result = error1.Equals(error2);

        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidData))]
    public void GetHashCode_WhenTwoInstancesHaveSameValues_ShouldReturnSameHashCode(
        string code,
        string description,
        int numericType,
        Dictionary<string, object>? metadata)
    {
        var error1 = Error.Custom(numericType, code, description, metadata);
        var clonedDictionary = metadata is null ? null : new Dictionary<string, object>(metadata);
        var error2 = Error.Custom(numericType, code, description, clonedDictionary);

        var hashCode1 = error1.GetHashCode();
        var hashCode2 = error2.GetHashCode();

        Assert.Equal(hashCode2, hashCode1);
    }

    [Theory]
    [MemberData(nameof(DifferentInstances))]
    public void GetHashCode_WhenTwoInstancesHaveDifferentValues_ShouldReturnDifferentHashCodes(
        Error error1,
        Error error2)
    {
        var hashCode1 = error1.GetHashCode();
        var hashCode2 = error2.GetHashCode();

        Assert.NotEqual(hashCode2, hashCode1);
    }
}
