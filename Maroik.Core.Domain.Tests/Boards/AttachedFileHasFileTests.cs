using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Domain.Tests.Boards;

/// <summary>An edit that removes an attachment empties its record: that record has no file.</summary>
public class AttachedFileHasFileTests
{
    /// <summary>A post's record with a name and a path has a file; a cleared one has none.</summary>
    [Fact]
    public void ABoardAttachment_HasNoFile_OnceCleared()
    {
        BoardAttachedFile file = BoardAttachedFile.Reconstitute(1, 7, 4, "report", ".zip", "upload/x.zip");

        Assert.True(file.HasFile);
        Assert.False(file.Cleared().HasFile);
    }

    /// <summary>An event's record with a name and a path has a file; a cleared one has none.</summary>
    [Fact]
    public void AnEventAttachment_HasNoFile_OnceCleared()
    {
        CalendarEventAttachedFile file = CalendarEventAttachedFile.Reconstitute(1, 7, 4, "report", ".zip", "upload/x.zip");

        Assert.True(file.HasFile);
        Assert.False(file.Cleared().HasFile);
    }
}
