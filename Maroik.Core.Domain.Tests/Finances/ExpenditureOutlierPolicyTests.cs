using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="ExpenditureOutlierPolicy"/>: which expenditures are unusually large for their sub-class and
/// currency (above the box-plot rule's upper fence, Q3 + 1.5 × IQR of the log amounts, quartiles by linear interpolation). The flag is shown next to the
/// amount in the expenditure list; it changes nothing that is stored.
/// </summary>
public class ExpenditureOutlierPolicyTests
{
    /// <summary>Ids are given in list order from 1.</summary>
    private static List<Expenditure> Records(string subClass, string currency, params decimal[] amounts) =>
        [.. amounts.Select((amount, i) => Record(i + 1, subClass, currency, amount))];

    /// <summary>A consumer-spending record <paramref name="id"/> of <paramref name="amount"/> in <paramref name="currency"/>.</summary>
    private static Expenditure Record(long id, string subClass, string currency, decimal amount) =>
        Expenditure.Reconstitute(id, "user@example.com", "ConsumerSpending", subClass, "x", amount, currency, "Wallet", null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    /// <summary>One amount far above seven ordinary ones is flagged, and only it.</summary>
    [Fact]
    public void UnusuallyLarge_FlagsTheAmountFarAboveTheRest()
    {
        var result = ExpenditureOutlierPolicy.UnusuallyLarge(Records("MealOrEatOutExpenses", "KRW", 10, 10, 11, 12, 12, 13, 14, 100));

        Assert.Equal([8L], result);
    }

    /// <summary>Below <see cref="ExpenditureOutlierPolicy.MinimumGroupSize"/> records there is no "usual" yet, so nothing is flagged.</summary>
    [Fact]
    public void UnusuallyLarge_FlagsNothing_BelowTheMinimumGroupSize()
    {
        Assert.Equal(8, ExpenditureOutlierPolicy.MinimumGroupSize);
        Assert.Empty(ExpenditureOutlierPolicy.UnusuallyLarge(Records("MealOrEatOutExpenses", "KRW", 10, 10, 11, 12, 12, 13, 100)));
    }

    /// <summary>
    /// The fence is drawn on the logarithm of the amounts, so "unusual" is a ratio, not a difference: around 100–160 the fence
    /// lies at about 225 (on the raw amounts it would be 205). 215 is ordinary; 240 is not.
    /// </summary>
    [Theory]
    [InlineData(215, false)]
    [InlineData(240, true)]
    public void UnusuallyLarge_JudgesTheRatio_OnTheLogarithmOfTheAmounts(int last, bool flagged)
    {
        var result = ExpenditureOutlierPolicy.UnusuallyLarge(Records("MealOrEatOutExpenses", "KRW", 100, 110, 120, 130, 140, 150, 160, last));

        Assert.Equal(flagged, result.Contains(8));
    }

    /// <summary>
    /// When every usual amount is the same (a fixed rent), the spread is zero and anything above that amount is unusual,
    /// in either order of the records.
    /// </summary>
    [Fact]
    public void UnusuallyLarge_WithNoSpread_FlagsAnythingAboveTheUsualAmount()
    {
        Assert.Empty(ExpenditureOutlierPolicy.UnusuallyLarge(Records("HousingOrSuppliesCost", "KRW", 500, 500, 500, 500, 500, 500, 500, 500)));
        Assert.Equal([1L], ExpenditureOutlierPolicy.UnusuallyLarge(Records("HousingOrSuppliesCost", "KRW", 600, 500, 500, 500, 500, 500, 500, 500)));
    }

    /// <summary>
    /// Each sub-class has its own "usual": a 900 rent among rents of 900 is not unusual just because meals cost 10, and a 100
    /// meal is unusual among meals even though rents are larger.
    /// </summary>
    [Fact]
    public void UnusuallyLarge_JudgesEachSubClassOnItsOwn()
    {
        List<Expenditure> history =
        [
            .. Records("MealOrEatOutExpenses", "KRW", 10, 10, 11, 12, 12, 13, 14, 100),
            .. Records("HousingOrSuppliesCost", "KRW", 900, 900, 900, 900, 900, 900, 900, 900).Select(e =>
                Record(e.Id + 100, e.SubClass, "KRW", e.Amount.Amount)),
        ];

        Assert.Equal([8L], ExpenditureOutlierPolicy.UnusuallyLarge(history));
    }

    /// <summary>
    /// Each currency label has its own "usual" (there is no conversion): eight KRW meals of 10,000 do not make a USD meal of
    /// 10 small, nor the other way round, and a group of each currency below the minimum flags nothing.
    /// </summary>
    [Fact]
    public void UnusuallyLarge_JudgesEachCurrencyOnItsOwn()
    {
        List<Expenditure> history =
        [
            .. Records("MealOrEatOutExpenses", "USD", 10, 10, 11, 12),
            .. Records("MealOrEatOutExpenses", "KRW", 10000, 10000, 11000, 12000).Select(e => Record(e.Id + 100, e.SubClass, "KRW", e.Amount.Amount)),
        ];

        Assert.Empty(ExpenditureOutlierPolicy.UnusuallyLarge(history));
    }

    /// <summary>No records, no flags.</summary>
    [Fact]
    public void UnusuallyLarge_FlagsNothing_ForNoRecords()
    {
        Assert.Empty(ExpenditureOutlierPolicy.UnusuallyLarge([]));
    }
}
