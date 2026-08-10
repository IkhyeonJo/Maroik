using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="OtherCalendar"/>.
/// Covers creation validation (email format, calendar ID guard) and reconstitution from trusted data.
/// </summary>
public class OtherCalendarTests
{
    // -- Create ---------------------------------------------------------------

    [Fact]
    public void Create_ReturnsOtherCalendar_WhenValid()
    {
        var result = OtherCalendar.Create("sub@example.com", 1);

        Assert.False(result.IsError);
        Assert.Equal("sub@example.com", result.Value.AccountEmail.Value);
        Assert.Equal(1, result.Value.CalendarId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Create_ReturnsError_WhenEmailInvalid(string? email)
    {
        var result = OtherCalendar.Create(email, 1);

        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ReturnsError_WhenCalendarIdNotPositive(long calendarId)
    {
        var result = OtherCalendar.Create("sub@example.com", calendarId);

        Assert.True(result.IsError);
        Assert.Equal("OtherCalendar.InvalidCalendarId", result.FirstError.Code);
    }

    // -- Reconstitute -----------------------------------------------------------

    [Fact]
    public void Reconstitute_RebuildsOtherCalendar_WithoutValidation()
    {
        var otherCalendar = OtherCalendar.Reconstitute("sub@example.com", 1);

        Assert.Equal("sub@example.com", otherCalendar.AccountEmail.Value);
        Assert.Equal(1, otherCalendar.CalendarId);
        Assert.Equal(("sub@example.com", 1L), otherCalendar.Id);
    }
}
