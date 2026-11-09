using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Domain.Tests.Uploads;

/// <summary>The storage keys of uploaded files; a stored file's path must not change, or its record would point at nothing.</summary>
public class AttachmentStoragePathsTests
{
    /// <summary>A fixed id.</summary>
    private static readonly Guid _id = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    /// <summary>A private note is stored under Management and every other post under Forum, by an upper-case GUID with the original extension.</summary>
    [Theory]
    [InlineData("PrivateNote", "upload/Management/PrivateNote/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("FreeForum", "upload/Forum/FreeForum/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("Unknown", "upload/Forum/Unknown/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    public void BoardAttachment_IsStoredUnderTheBoardsArea(string boardType, string expected)
    {
        Assert.Equal(expected, AttachmentStoragePaths.BoardAttachment(boardType, 7, _id, "report.zip"));
    }

    /// <summary>The original name goes no further than its extension, so it cannot traverse the path.</summary>
    [Fact]
    public void AnAttachment_KeepsOnlyTheExtensionOfTheOriginalName()
    {
        Assert.Equal("upload/Calendar/User/calendarEventAttachedFiles/9/0F8FAD5B-D9CB-469F-A165-70867728950E.txt",
            AttachmentStoragePaths.CalendarEventAttachment("User", 9, _id, "../../evil.txt"));
    }

    /// <summary>An editor image lives under its page's area.</summary>
    [Fact]
    public void EditorImage_IsStoredUnderThePage()
    {
        Assert.Equal("upload/Forum/FreeForum/summernote/images/a.png", AttachmentStoragePaths.EditorImage("Forum", "FreeForum", "a.png"));
    }
}
