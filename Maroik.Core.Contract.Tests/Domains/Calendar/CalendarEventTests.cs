using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Tests.Domains.CalendarDomain;

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

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsCalendarEvent_WhenValid()
    {
        var result = CalendarEvent.Create(10, "Stand-up", null, false, Start, End, "UTC", "UTC", null, null);

        Assert.False(result.IsError);
        Assert.Equal("Stand-up", result.Value.Title);
        Assert.Equal(10, result.Value.CalendarId);
    }

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

    [Fact]
    public void Create_ReturnsError_WhenTitleTooLong()
    {
        var result = CalendarEvent.Create(10, new string('x', 101), null, false, Start, End, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.TitleTooLong", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenDescriptionTooLong()
    {
        var result = CalendarEvent.Create(10, "Title", new string('x', 16385), false, Start, End, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.DescriptionTooLong", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenEndBeforeStart_AndNotAllDay()
    {
        var result = CalendarEvent.Create(10, "Title", null, false, End, Start, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidDateRange", result.FirstError.Code);
    }

    [Fact]
    public void Create_Succeeds_WhenEndBeforeStart_AndAllDay()
    {
        var result = CalendarEvent.Create(10, "All Day", null, true, End, Start, null, null, null, null);

        Assert.False(result.IsError);
    }

    // ── Update ───────────────────────────────────────────────────────────────

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

    [Fact]
    public void Update_ReturnsError_WhenEndBeforeStart()
    {
        var ev = ValidEvent();

        var result = ev.Update("Title", null, false, End, Start, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidDateRange", result.FirstError.Code);
    }

    // ── AddReminder ───────────────────────────────────────────────────────────

    [Fact]
    public void AddReminder_Succeeds_WhenReminderMatchesEvent()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AddReminder(ValidReminder(eventId: 1));

        Assert.False(result.IsError);
        Assert.Single(ev.Reminders);
    }

    [Fact]
    public void AddReminder_ReturnsError_WhenMismatch()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AddReminder(ValidReminder(eventId: 99));

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.ReminderMismatch", result.FirstError.Code);
    }

    // ── RemoveReminder ────────────────────────────────────────────────────────

    [Fact]
    public void RemoveReminder_Succeeds_WhenReminderExists()
    {
        var ev = ValidEvent(id: 1);
        ev.AddReminder(ValidReminder(eventId: 1));

        var result = ev.RemoveReminder(1);

        Assert.False(result.IsError);
        Assert.Empty(ev.Reminders);
    }

    [Fact]
    public void RemoveReminder_ReturnsError_WhenNotFound()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.RemoveReminder(999);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.ReminderNotFound", result.FirstError.Code);
    }

    // ── AttachFile ────────────────────────────────────────────────────────────

    [Fact]
    public void AttachFile_Succeeds_WhenFileMatchesEvent()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AttachFile(ValidFile(eventId: 1));

        Assert.False(result.IsError);
        Assert.Single(ev.AttachedFiles);
    }

    [Fact]
    public void AttachFile_ReturnsError_WhenMismatch()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.AttachFile(ValidFile(eventId: 99));

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.FileMismatch", result.FirstError.Code);
    }

    // ── RemoveFile ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveFile_Succeeds_WhenFileExists()
    {
        var ev = ValidEvent(id: 1);
        ev.AttachFile(ValidFile(eventId: 1));

        var result = ev.RemoveFile(1);

        Assert.False(result.IsError);
        Assert.Empty(ev.AttachedFiles);
    }

    [Fact]
    public void RemoveFile_ReturnsError_WhenNotFound()
    {
        var ev = ValidEvent(id: 1);

        var result = ev.RemoveFile(999);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.FileNotFound", result.FirstError.Code);
    }
}
