using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Calendar.Calendar"/>.
/// Covers calendar creation and update.
/// </summary>
public class CalendarTests
{
    private static Calendar ValidCalendar(long id = 1) =>
        Calendar.Reconstitute(id, "user@example.com", "Work", "My work calendar", "UTC", "#3498DB", DateTime.UtcNow, DateTime.UtcNow);

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

}
