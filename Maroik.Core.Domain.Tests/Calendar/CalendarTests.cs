using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Calendar.Calendar"/>.
/// Covers calendar creation, update, sharing settings, and subscriber management.
/// </summary>
public class CalendarTests
{
    private static Calendar ValidCalendar(long id = 1) =>
        Calendar.Reconstitute(id, "user@example.com", "Work", "My work calendar", "UTC", "#3498DB", DateTime.UtcNow, DateTime.UtcNow);

    private static OtherCalendar ValidSubscription(string email = "sub@example.com", long calendarId = 1) =>
        OtherCalendar.Create(email, calendarId).Value;

    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns calendar, when valid.</summary>
    [Fact]
    public void Create_ReturnsCalendar_WhenValid()
    {
        var result = Calendar.Create("user@example.com", "Personal", null, "UTC", "#FF0000");

        Assert.False(result.IsError);
        Assert.Equal("Personal", result.Value.Name);
        Assert.Equal("user@example.com", result.Value.AccountEmail.Value);
    }

    /// <summary>Create returns error, when name empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenNameEmpty(string? name)
    {
        var result = Calendar.Create("user@example.com", name, null, "UTC", "#FF0000");

        Assert.True(result.IsError);
        Assert.Equal("Calendar.NameEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when email invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenEmailInvalid()
    {
        var result = Calendar.Create("bademail", "Work", null, "UTC", "#FF0000");

        Assert.True(result.IsError);
    }

    /// <summary>Create returns error, when time zone invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenTimeZoneInvalid()
    {
        var result = Calendar.Create("user@example.com", "Work", null, "Not/Valid", "#FF0000");

        Assert.True(result.IsError);
        Assert.Equal("TimeZoneId.Invalid", result.FirstError.Code);
    }

    /// <summary>Create returns error, when color code invalid.</summary>
    [Fact]
    public void Create_ReturnsError_WhenColorCodeInvalid()
    {
        var result = Calendar.Create("user@example.com", "Work", null, "UTC", "red");

        Assert.True(result.IsError);
        Assert.Equal("HtmlColorCode.Invalid", result.FirstError.Code);
    }

    // -- Update ---------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var calendar = ValidCalendar();

        var result = calendar.Update("Personal", "Updated desc", "Asia/Seoul", "#E74C3C");

        Assert.False(result.IsError);
        Assert.Equal("Personal", calendar.Name);
        Assert.Equal("Asia/Seoul", calendar.TimeZone.Value);
        Assert.Equal("#E74C3C", calendar.ColorCode.Value);
    }

    /// <summary>Update returns error, when name empty.</summary>
    [Fact]
    public void Update_ReturnsError_WhenNameEmpty()
    {
        var calendar = ValidCalendar();

        var result = calendar.Update("", null, "UTC", "#FF0000");

        Assert.True(result.IsError);
        Assert.Equal("Calendar.NameEmpty", result.FirstError.Code);
    }

    // -- UpdateSharing ---------------------------------------------------------

    /// <summary>Update sharing creates shared, when shared is null.</summary>
    [Fact]
    public void UpdateSharing_CreatesShared_WhenSharedIsNull()
    {
        var calendar = ValidCalendar();
        Assert.Null(calendar.Shared);

        calendar.UpdateSharing(true, false);

        Assert.NotNull(calendar.Shared);
        Assert.True(calendar.Shared!.User);
        Assert.False(calendar.Shared.Anonymous);
    }

    /// <summary>Update sharing updates existing shared.</summary>
    [Fact]
    public void UpdateSharing_UpdatesExistingShared()
    {
        var calendar = ValidCalendar();
        calendar.UpdateSharing(true, false);

        calendar.UpdateSharing(true, true);

        Assert.True(calendar.Shared!.Anonymous);
    }

    // -- AddSubscriber ---------------------------------------------------------

    /// <summary>Add subscriber succeeds, when not already subscribed.</summary>
    [Fact]
    public void AddSubscriber_Succeeds_WhenNotAlreadySubscribed()
    {
        var calendar = ValidCalendar(id: 1);
        var subscription = ValidSubscription("sub@example.com", calendarId: 1);

        var result = calendar.AddSubscriber(subscription);

        Assert.False(result.IsError);
        Assert.Single(calendar.Subscribers);
    }

    /// <summary>Add subscriber returns error, when already subscribed.</summary>
    [Fact]
    public void AddSubscriber_ReturnsError_WhenAlreadySubscribed()
    {
        var calendar = ValidCalendar(id: 1);
        calendar.AddSubscriber(ValidSubscription("sub@example.com", calendarId: 1));

        var result = calendar.AddSubscriber(ValidSubscription("sub@example.com", calendarId: 1));

        Assert.True(result.IsError);
        Assert.Equal("Calendar.AlreadySubscribed", result.FirstError.Code);
    }

    /// <summary>Add subscriber returns error, when calendar id mismatch.</summary>
    [Fact]
    public void AddSubscriber_ReturnsError_WhenCalendarIdMismatch()
    {
        var calendar = ValidCalendar(id: 1);
        var subscription = ValidSubscription("sub@example.com", calendarId: 99);

        var result = calendar.AddSubscriber(subscription);

        Assert.True(result.IsError);
        Assert.Equal("Calendar.SubscriberMismatch", result.FirstError.Code);
    }

    // -- RemoveSubscriber ------------------------------------------------------

    /// <summary>Remove subscriber succeeds, when subscriber exists.</summary>
    [Fact]
    public void RemoveSubscriber_Succeeds_WhenSubscriberExists()
    {
        var calendar = ValidCalendar(id: 1);
        calendar.AddSubscriber(ValidSubscription("sub@example.com", calendarId: 1));

        var result = calendar.RemoveSubscriber("sub@example.com");

        Assert.False(result.IsError);
        Assert.Empty(calendar.Subscribers);
    }

    /// <summary>Remove subscriber returns error, when not found.</summary>
    [Fact]
    public void RemoveSubscriber_ReturnsError_WhenNotFound()
    {
        var calendar = ValidCalendar(id: 1);

        var result = calendar.RemoveSubscriber("nobody@example.com");

        Assert.True(result.IsError);
        Assert.Equal("Calendar.SubscriberNotFound", result.FirstError.Code);
    }
}
