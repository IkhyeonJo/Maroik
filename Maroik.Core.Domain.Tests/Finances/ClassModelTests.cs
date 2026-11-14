using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="ClassModel"/>, the record of an account's trained class-suggestion model of one book: where its file
/// is stored, whether it still answers for the account's records, and what a retraining changes.
/// </summary>
public class ClassModelTests
{
    /// <summary>When the model was trained in these tests.</summary>
    private static readonly DateTime _trained = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A model of alice's expenditures stored at <c>upload/ClassModels/{id}.zip</c>, trained on records "fp1" with version "v1".</summary>
    private static ClassModel Model() =>
        ClassModel.Create("alice@test.com", ClassModel.ExpenditureBook, "upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip", "fp1", "v1", _trained);

    /// <summary>A model's file lives under <c>upload/ClassModels</c>, named by a GUID, as a zip.</summary>
    [Fact]
    public void StoragePathFor_NamesTheFileByItsGuid()
    {
        Assert.Equal("upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip",
            ClassModel.StoragePathFor(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")));
    }

    /// <summary>Only a path of that exact shape is a model file: the storage service deletes nothing else through the model API.</summary>
    [Theory]
    [InlineData("upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip", true)]
    [InlineData("upload/ClassModels/0F8FAD5B-D9CB-469F-A165-70867728950E.zip", false)]
    [InlineData("upload/ClassModels/0f8fad5bd9cb469fa16570867728950e.zip", false)]
    [InlineData("upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e", false)]
    [InlineData("upload/ClassModels/../Forum/0f8fad5b-d9cb-469f-a165-70867728950e.zip", false)]
    [InlineData("upload/FileManager/0f8fad5b-d9cb-469f-a165-70867728950e", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStoragePath_AcceptsOnlyAModelFile(string? path, bool expected)
    {
        Assert.Equal(expected, ClassModel.IsStoragePath(path));
    }

    /// <summary>A model answers for the records it was trained on, with the model version the code runs.</summary>
    [Theory]
    [InlineData("fp1", "v1", true)]
    [InlineData("fp2", "v1", false)]
    [InlineData("fp1", "v2", false)]
    public void IsCurrent_NeedsTheSameRecordsAndVersion(string fingerprint, string version, bool expected)
    {
        Assert.Equal(expected, Model().IsCurrent(fingerprint, version));
    }

    /// <summary>
    /// An older model of the same version still answers while its successor trains (the records changed a little); one of another
    /// version cannot be read by this code at all.
    /// </summary>
    [Theory]
    [InlineData("v1", true)]
    [InlineData("v2", false)]
    public void IsReadableBy_NeedsTheSameVersion(string version, bool expected)
    {
        Assert.Equal(expected, Model().IsReadableBy(version));
    }

    /// <summary>A retraining keeps the record (id, account, book) and replaces the file, the records, the version and the time.</summary>
    [Fact]
    public void Retrained_ReplacesTheFileAndWhatItWasTrainedOn()
    {
        ClassModel kept = ClassModel.Reconstitute(7, "alice@test.com", ClassModel.IncomeBook, "upload/ClassModels/a.zip", "fp1", "v1", _trained);

        ClassModel next = kept.Retrained("upload/ClassModels/b.zip", "fp2", "v2", _trained.AddHours(1));

        Assert.Equal((7L, "alice@test.com", ClassModel.IncomeBook, "upload/ClassModels/b.zip", "fp2", "v2", _trained.AddHours(1)),
            (next.Id, next.AccountEmail, next.Book, next.FilePath, next.Fingerprint, next.ModelVersion, next.Trained));
    }

    /// <summary>The two books a model is trained for, as the database's CHECK names them.</summary>
    [Fact]
    public void Books_AreIncomeAndExpenditure()
    {
        Assert.Equal(("Income", "Expenditure"), (ClassModel.IncomeBook, ClassModel.ExpenditureBook));
    }
}
