using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Calendar.CalendarEvent"/>.
/// Covers event creation and update.
/// </summary>
public class CalendarEventTests
{
    private static readonly DateTime Start = new(2025, 6, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static CalendarEvent ValidEvent(long id = 1, long calendarId = 10) =>
        CalendarEvent.Reconstitute(id, calendarId, "Team Meeting", null, false, Start, End, "UTC", "UTC", null, null, null, DateTime.UtcNow, DateTime.UtcNow);

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

    /// <summary>Create succeeds when the end time is before the start time on the same day and the event is all-day.</summary>
    [Fact]
    public void Create_Succeeds_WhenEndBeforeStart_AndAllDay()
    {
        var result = CalendarEvent.Create(10, "All Day", null, true, End, Start, null, null, null, null);

        Assert.False(result.IsError);
    }

    /// <summary>Create rejects an all-day event whose end date is on an earlier day than the start date.</summary>
    [Fact]
    public void Create_ReturnsError_WhenEndDateBeforeStartDate_AndAllDay()
    {
        var result = CalendarEvent.Create(10, "All Day", null, true, Start, Start.AddDays(-1), null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidDateRange", result.FirstError.Code);
    }

    /// <summary>Create rejects an unrecognised per-endpoint time-zone ID.</summary>
    [Fact]
    public void Create_ReturnsError_WhenTimeZoneIdInvalid()
    {
        var result = CalendarEvent.Create(10, "Title", null, false, Start, End, "Not/AZone", "UTC", null, null);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.InvalidTimeZone", result.FirstError.Code);
    }

    /// <summary>
    /// The rejection is built via <c>LocalizableError</c>: its Metadata carries the composite-format
    /// resource template and the raw offending zone separately, so <c>ServiceResult.FromError</c> can
    /// hand the UI something resx-localizable instead of the already-baked, value-embedding sentence
    /// in <see cref="ErrorOr.Error.Description"/>.
    /// </summary>
    [Fact]
    public void Create_ReturnsError_WithLocalizableMetadata_WhenTimeZoneIdInvalid()
    {
        var result = CalendarEvent.Create(10, "Title", null, false, Start, End, "Not/AZone", "UTC", null, null);

        Assert.Equal("'Not/AZone' is not a recognised time-zone ID.", result.FirstError.Description);
        Assert.Equal("'{0}' is not a recognised time-zone ID.", result.FirstError.Metadata!["ResourceKey"]);
        Assert.Equal(["Not/AZone"], (object[])result.FirstError.Metadata["ResourceArgs"]);
    }

    /// <summary>
    /// Regression: Create rejects a <c>Status</c> value outside <see cref="CalendarEventStatuses"/>'s
    /// known members — previously any non-empty string was accepted here and would only fail later
    /// with a raw DB <c>CalendarEvent_Status_check</c> exception on save. A null/blank status is
    /// still allowed (the repository maps it to the "Busy" default before persisting).
    /// </summary>
    [Theory]
    [InlineData("Tentative")]
    [InlineData("busy")]
    public void Create_ReturnsError_WhenStatusNotRecognised(string status)
    {
        var result = CalendarEvent.Create(10, "Title", null, false, Start, End, null, null, null, status);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.StatusInvalid", result.FirstError.Code);
    }

    /// <summary>Create still succeeds with a null or blank status -- the repository defaults it before persisting.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Succeeds_WhenStatusNullOrBlank(string? status)
    {
        var result = CalendarEvent.Create(10, "Title", null, false, Start, End, null, null, null, status);

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

    /// <summary>Regression: Update rejects a <c>Status</c> value outside <see cref="CalendarEventStatuses"/>'s known members (see <see cref="Create_ReturnsError_WhenStatusNotRecognised"/>).</summary>
    [Fact]
    public void Update_ReturnsError_WhenStatusNotRecognised()
    {
        var ev = ValidEvent();

        var result = ev.Update("Title", null, false, Start, End, null, null, null, "Tentative");

        Assert.True(result.IsError);
        Assert.Equal("CalendarEvent.StatusInvalid", result.FirstError.Code);
    }

}
