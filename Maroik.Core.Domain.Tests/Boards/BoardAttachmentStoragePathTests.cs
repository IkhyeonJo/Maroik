using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Domain.Tests.Boards;

/// <summary>Where a post's attachment is stored; a stored file's path must not change, or its record would point at nothing.</summary>
public class BoardAttachmentStoragePathTests
{
    /// <summary>A private note is stored under Management and every other post under Forum, by an upper-case GUID with the original extension.</summary>
    [Theory]
    [InlineData("PrivateNote", "upload/Management/PrivateNote/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("privatenote", "upload/Management/privatenote/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("FreeForum", "upload/Forum/FreeForum/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    [InlineData("Unknown", "upload/Forum/Unknown/boardAttachedFiles/7/0F8FAD5B-D9CB-469F-A165-70867728950E.zip")]
    public void StoragePathFor_IsUnderTheBoardsArea(string boardType, string expected)
    {
        Assert.Equal(expected, BoardAttachedFile.StoragePathFor(boardType, 7, new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), "report.zip"));
    }
}
