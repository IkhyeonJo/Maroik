using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Calendars;
using DomainBoard = Maroik.Core.Domain.Boards.Board;
using DomainCalendar = Maroik.Core.Domain.Calendars.Calendar;
namespace Maroik.Core.Domain.Tests.Invariants;

/// <summary>
/// The domain never reads the clock: every factory stamps Created and Updated with the one instant
/// passed in (so they cannot drift apart), and every later change stamps only Updated with its own
/// given instant.
/// </summary>
public class ClockTests
{
    /// <summary>The creation instant passed to the factories.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A later instant passed to a state change.</summary>
    private static readonly DateTime _later = _now.AddMinutes(5);

    /// <summary>Calendar: create stamps both, update stamps Updated only.</summary>
    [Fact]
    public void Calendar_CreateAndUpdate_UseTheGivenInstants()
    {
        DomainCalendar calendar = DomainCalendar.Create("a@b.com", "Work", null, "UTC", "#112233", _now).Value;
        Assert.Equal((_now, _now), (calendar.Created, calendar.Updated));

        Assert.False(calendar.Update("Home", null, "UTC", "#112233", _later).IsError);
        Assert.Equal((_now, _later), (calendar.Created, calendar.Updated));
    }

    /// <summary>Calendar event: create stamps both; update and reassign stamp Updated only.</summary>
    [Fact]
    public void CalendarEvent_CreateUpdateAndReassign_UseTheGivenInstants()
    {
        DateTime start = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        CalendarEvent ev = CalendarEvent.Create(10, "Stand-up", null, false, start, start.AddHours(1), null, null, null, null, _now).Value;
        Assert.Equal((_now, _now), (ev.Created, ev.Updated));

        Assert.False(ev.Update("Stand-up", null, false, start, start.AddHours(2), null, null, null, null, _later).IsError);
        Assert.Equal((_now, _later), (ev.Created, ev.Updated));

        ev.Reassign(11, null, _later.AddMinutes(1));
        Assert.Equal(_later.AddMinutes(1), ev.Updated);
    }

    /// <summary>Board: create stamps both; every state change stamps Updated only.</summary>
    [Fact]
    public void Board_CreateAndChanges_UseTheGivenInstants()
    {
        DomainBoard board = DomainBoard.Create(BoardTypes.FreeForum, "t", "c", "Alice", _now).Value;
        Assert.Equal((_now, _now), (board.Created, board.Updated));

        board.Lock(_later);
        Assert.Equal((_now, _later), (board.Created, board.Updated));
        board.Unlock(_later.AddMinutes(1));
        Assert.Equal(_later.AddMinutes(1), board.Updated);
        board.Pin(_later.AddMinutes(2));
        Assert.Equal(_later.AddMinutes(2), board.Updated);
        Assert.False(board.Update("t2", "c2", _later.AddMinutes(4)).IsError);
        Assert.Equal(_later.AddMinutes(4), board.Updated);
        Assert.False(board.SoftDelete(_later.AddMinutes(5)).IsError);
        Assert.Equal(_later.AddMinutes(5), board.Updated);
    }

    /// <summary>A board comment's Created is the instant passed in.</summary>
    [Fact]
    public void BoardComment_Create_UsesTheGivenInstant()
        => Assert.Equal(_now, BoardComment.Create(1, 1, null, "Bob", "hi", _now).Value.Created);
}
