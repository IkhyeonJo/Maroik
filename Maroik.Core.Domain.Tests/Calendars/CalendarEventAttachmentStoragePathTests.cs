using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Domain.Tests.Calendars;

/// <summary>Where an event's attachment is stored; a stored file's path must not change, or its record would point at nothing.</summary>
public class CalendarEventAttachmentStoragePathTests
{
    /// <summary>An event's attachment is stored in the event's own folder, and the original name goes no further than its extension.</summary>
    [Fact]
    public void StoragePathFor_IsInTheEventsOwnFolder()
    {
        Assert.Equal("upload/Calendar/User/calendarEventAttachedFiles/9/0F8FAD5B-D9CB-469F-A165-70867728950E.txt",
            CalendarEventAttachedFile.StoragePathFor("User", 9, new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), "../../evil.txt"));
    }
}
