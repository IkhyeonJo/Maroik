using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Calendar.CalendarEvent"/>.
/// Covers event creation, update, reminder management, and file attachment.
/// </summary>
public class CalendarEventTests
{
    private static readonly DateTime Start = new(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static CalendarEvent ValidEvent(long id = 1, long calendarId = 10) =>
        CalendarEvent.Reconstitute(id, calendarId, "Team Meeting", null, false, Start, End, "UTC", "UTC", null, null, null, DateTime.UtcNow, DateTime.UtcNow);

    private static CalendarEventReminder ValidReminder(long eventId = 1) =>
        CalendarEventReminder.Reconstitute(1, eventId, "Email", 30, null, null, null, null);

    private static CalendarEventAttachedFile ValidFile(long eventId = 1) =>
        CalendarEventAttachedFile.Reconstitute(1, eventId, 512, "agenda.pdf", ".pdf", "/files/agenda.pdf");

    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns calendar event, when valid.</summary>
    [Fact]
    public void Create_ReturnsCalendarEvent_WhenValid()
    {
        var result = CalendarEvent.Create(10, "Stand-up", null, false, Start, End, "UTC", "UTC", null, null);

        Assert.False(result.IsError);
        Assert.Equal("Stand-up", result.Value.Title);
        Assert.Equal(10, result.Value.CalendarId);
    }

    /// <summary>Create returns error, when title empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenTitleEmpty(string? title)
    {
        var result = CalendarEvent.Create(10, title, null, false, Start, End, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.TitleEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when title too long.</summary>
    [Fact]
    public void Create_ReturnsError_WhenTitleTooLong()
    {
        var result = CalendarEvent.Create(10, new string('x', 101), null, false, Start, End, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.TitleTooLong", result.FirstError.Code);
    }

    /// <summary>Create returns error, when description too long.</summary>
    [Fact]
    public void Create_ReturnsError_WhenDescriptionTooLong()
    {
        var result = CalendarEvent.Create(10, "Title", new string('x', 16385), false, Start, End, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.DescriptionTooLong", result.FirstError.Code);
    }

    /// <summary>Create returns error when end before start and not all day.</summary>
    [Fact]
    public void Create_ReturnsError_WhenEndBeforeStart_AndNotAllDay()
    {
        var result = CalendarEvent.Create(10, "Title", null, false, End, Start, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidDateRange", result.FirstError.Code);
    }

    /// <summary>Create succeeds when end before start and all day.</summary>
    [Fact]
    public void Create_Succeeds_WhenEndBeforeStart_AndAllDay()
    {
        var result = CalendarEvent.Create(10, "All Day", null, true, End, Start, null, null, null, null);

        Assert.False(result.IsError);
    }

    // -- Update ---------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var ev = ValidEvent();
        var newEnd = End.AddHours(1);

        var result = ev.Update("Updated Title", "desc", false, Start, newEnd, "Asia/Seoul", "Asia/Seoul", "Room 1", "Busy");

        Assert.False(result.IsError);
        Assert.Equal("Updated Title", ev.Title);
        Assert.Equal("Room 1", ev.Location);
    }

    /// <summary>Update returns error, when end before start.</summary>
    [Fact]
    public void Update_ReturnsError_WhenEndBeforeStart()
    {
        var ev = ValidEvent();

        var result = ev.Update("Title", null, false, End, Start, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidDateRange", result.FirstError.Code);
    }

    // -- AddReminder -----------------------------------------------------------

    /// <summary>Add reminder succeeds, when reminder matches event.</summary>
    [Fact]
    public void AddReminder_Succeeds_WhenReminderMatchesEvent()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AddReminder(ValidReminder(eventId: 1));

        Assert.False(result.IsError);
        Assert.Single(ev.Reminders);
    }

    /// <summary>Add reminder returns error, when mismatch.</summary>
    [Fact]
    public void AddReminder_ReturnsError_WhenMismatch()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AddReminder(ValidReminder(eventId: 99));

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.ReminderMismatch", result.FirstError.Code);
    }

    // -- RemoveReminder --------------------------------------------------------

    /// <summary>Remove reminder succeeds, when reminder exists.</summary>
    [Fact]
    public void RemoveReminder_Succeeds_WhenReminderExists()
    {
        var ev = ValidEvent(id: 1);
        ev.AddReminder(ValidReminder(eventId: 1));

        var result = ev.RemoveReminder(1);

        Assert.False(result.IsError);
        Assert.Empty(ev.Reminders);
    }

    /// <summary>Remove reminder returns error, when not found.</summary>
    [Fact]
    public void RemoveReminder_ReturnsError_WhenNotFound()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.RemoveReminder(999);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.ReminderNotFound", result.FirstError.Code);
    }

    // -- AttachFile ------------------------------------------------------------

    /// <summary>Attach file succeeds, when file matches event.</summary>
    [Fact]
    public void AttachFile_Succeeds_WhenFileMatchesEvent()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AttachFile(ValidFile(eventId: 1));

        Assert.False(result.IsError);
        Assert.Single(ev.AttachedFiles);
    }

    /// <summary>Attach file returns error, when mismatch.</summary>
    [Fact]
    public void AttachFile_ReturnsError_WhenMismatch()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AttachFile(ValidFile(eventId: 99));

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.FileMismatch", result.FirstError.Code);
    }

    // -- RemoveFile ------------------------------------------------------------

    /// <summary>Remove file succeeds, when file exists.</summary>
    [Fact]
    public void RemoveFile_Succeeds_WhenFileExists()
    {
        var ev = ValidEvent(id: 1);
        ev.AttachFile(ValidFile(eventId: 1));

        var result = ev.RemoveFile(1);

        Assert.False(result.IsError);
        Assert.Empty(ev.AttachedFiles);
    }

    /// <summary>Remove file returns error, when not found.</summary>
    [Fact]
    public void RemoveFile_ReturnsError_WhenNotFound()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.RemoveFile(999);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.FileNotFound", result.FirstError.Code);
    }
}
