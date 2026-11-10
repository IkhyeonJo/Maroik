using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Domain.Tests.Uploads;

/// <summary>What the storage keys of uploaded files share; a stored file's path must not change, or its record would point at nothing.</summary>
public class AttachmentStoragePathsTests
{
    /// <summary>A stored attachment is named by an upper-case GUID, and the original name goes no further than its extension.</summary>
    [Theory]
    [InlineData("report.zip", "0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("../../evil.txt", "0F8FAD5B-D9CB-469F-A165-70867728950E.txt")]
    [InlineData("no-extension", "0F8FAD5B-D9CB-469F-A165-70867728950E")]
    public void StoredName_IsTheGuid_WithTheOriginalExtension(string fileName, string expected)
    {
        Assert.Equal(expected, AttachmentStoragePaths.StoredName(new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), fileName));
    }

    /// <summary>An editor image lives under its page's area.</summary>
    [Fact]
    public void EditorImage_IsStoredUnderThePage()
    {
        Assert.Equal("upload/Forum/FreeForum/summernote/images/a.png", AttachmentStoragePaths.EditorImage("Forum", "FreeForum", "a.png"));
    }
}
