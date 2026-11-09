using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Domain.Tests.Calendars;

/// <summary>Replacing or clearing an event's attachment keeps the record's identity and rewrites only its file fields.</summary>
public class CalendarEventAttachedFileChangeTests
{
    /// <summary>The stored attachment the changes start from.</summary>
    private static CalendarEventAttachedFile Stored() => CalendarEventAttachedFile.Reconstitute(7, 42, 100, "old", ".zip", "upload/old.zip");

    /// <summary>A cleared record keeps its id and event, and holds the empty-attachment values.</summary>
    [Fact]
    public void Cleared_KeepsTheIdentity_AndEmptiesTheFileFields()
    {
        CalendarEventAttachedFile cleared = Stored().Cleared();

        Assert.Equal(7, cleared.Id);
        Assert.Equal(42, cleared.CalendarEventId);
        Assert.Equal(0, cleared.Size);
        Assert.Equal("", cleared.Name);
        Assert.Equal("", cleared.Extension);
        Assert.Equal("", cleared.Path);
    }

    /// <summary>A replaced record keeps its id and takes every file field of the replacement.</summary>
    [Fact]
    public void ReplacedBy_KeepsTheId_AndTakesTheReplacementsFields()
    {
        CalendarEventAttachedFile replacement = CalendarEventAttachedFile.Create(42, 5, "new", ".png", "upload/new.png").Value;

        CalendarEventAttachedFile replaced = Stored().ReplacedBy(replacement);

        Assert.Equal(7, replaced.Id);
        Assert.Equal(42, replaced.CalendarEventId);
        Assert.Equal(5, replaced.Size);
        Assert.Equal("new", replaced.Name);
        Assert.Equal(".png", replaced.Extension);
        Assert.Equal("upload/new.png", replaced.Path);
    }
}
