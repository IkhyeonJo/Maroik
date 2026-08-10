using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="CalendarEventReminder"/>.
/// Covers creation validation (method empty guard, at-least-one-lead-time guard) and reconstitution from trusted data.
/// </summary>
public class CalendarEventReminderTests
{
    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns reminder, when valid.</summary>
    [Fact]
    public void Create_ReturnsReminder_WhenValid()
    {
        var result = CalendarEventReminder.Create(1, "Email", 30, null, null, null, null);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.CalendarEventId);
        Assert.Equal("Email", result.Value.Method);
        Assert.Equal(30, result.Value.MinutesBeforeEvent);
    }

    /// <summary>Create returns error, when method empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenMethodEmpty(string? method)
    {
        var result = CalendarEventReminder.Create(1, method, 30, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("Reminder.MethodEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when no lead time set.</summary>
    [Fact]
    public void Create_ReturnsError_WhenNoLeadTimeSet()
    {
        var result = CalendarEventReminder.Create(1, "Email", null, null, null, null, null);

        Assert.True(result.IsError);
        Assert.Equal("Reminder.NoLeadTime", result.FirstError.Code);
    }

    /// <summary>Create succeeds, when only hours before set.</summary>
    [Fact]
    public void Create_Succeeds_WhenOnlyHoursBeforeSet()
    {
        var result = CalendarEventReminder.Create(1, "Push", null, 2, null, null, null);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.HoursBeforeEvent);
    }

    /// <summary>Create succeeds, when only days before set.</summary>
    [Fact]
    public void Create_Succeeds_WhenOnlyDaysBeforeSet()
    {
        var result = CalendarEventReminder.Create(1, "Push", null, null, 1, null, null);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.DaysBeforeEvent);
    }

    /// <summary>Create succeeds, when only weeks before set.</summary>
    [Fact]
    public void Create_Succeeds_WhenOnlyWeeksBeforeSet()
    {
        var result = CalendarEventReminder.Create(1, "Push", null, null, null, 1, null);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.WeeksBeforeEvent);
    }

    /// <summary>Create allows optional times before event.</summary>
    [Fact]
    public void Create_AllowsOptionalTimesBeforeEvent()
    {
        var time = new TimeOnly(9, 0);

        var result = CalendarEventReminder.Create(1, "Email", 30, null, null, null, time);

        Assert.False(result.IsError);
        Assert.Equal(time, result.Value.TimesBeforeEvent);
    }

    // -- Reconstitute -----------------------------------------------------------

    /// <summary>Reconstitute rebuilds reminder without validation.</summary>
    [Fact]
    public void Reconstitute_RebuildsReminder_WithoutValidation()
    {
        var reminder = CalendarEventReminder.Reconstitute(5, 1, "Email", 30, null, null, null, null);

        Assert.Equal(5, reminder.Id);
        Assert.Equal(1, reminder.CalendarEventId);
        Assert.Equal("Email", reminder.Method);
        Assert.Equal(30, reminder.MinutesBeforeEvent);
    }

    /// <summary>Reconstitute defaults null method to empty string.</summary>
    [Fact]
    public void Reconstitute_DefaultsNullMethod_ToEmptyString()
    {
        var reminder = CalendarEventReminder.Reconstitute(1, 1, null, null, null, null, null, null);

        Assert.Equal("", reminder.Method);
    }
}
