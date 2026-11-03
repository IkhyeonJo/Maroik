using Maroik.Core.Domain.Finances;
namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>Unit tests for <see cref="FinanceHistoryPolicy"/>.</summary>
public class FinanceHistoryPolicyTests
{
    /// <summary>The fixed "current time".</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The history starts at the earlier of the two first records, whichever kind it is.</summary>
    [Fact]
    public void StartsAt_TheEarlierFirstRecord()
    {
        DateTime early = new(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime late = new(2023, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(early, FinanceHistoryPolicy.StartsAt(early, late, _now));
        Assert.Equal(early, FinanceHistoryPolicy.StartsAt(late, early, _now));
    }

    /// <summary>A missing record or a <see cref="DateTime.MinValue"/> stamp does not count.</summary>
    [Fact]
    public void StartsAt_IgnoresAMissingOrUnsetRecord()
    {
        DateTime recorded = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(recorded, FinanceHistoryPolicy.StartsAt(null, recorded, _now));
        Assert.Equal(recorded, FinanceHistoryPolicy.StartsAt(recorded, DateTime.MinValue, _now));
    }

    /// <summary>With nothing recorded the history starts now.</summary>
    [Fact]
    public void StartsAt_Now_WhenNothingIsRecorded()
        => Assert.Equal(_now, FinanceHistoryPolicy.StartsAt(null, DateTime.MinValue, _now));
}
