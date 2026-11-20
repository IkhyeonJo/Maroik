namespace Maroik.Core.Domain.Finances;

/// <summary>
/// Finds the expenditures that are unusually large for what they are: the box-plot rule's upper fence, judged
/// separately for every sub-class and currency label. The expenditure list marks them next to the amount so a typo (an extra
/// zero) or a one-off cost stands out; the mark changes nothing that is stored, and the user decides what it means.
/// <para>
/// A record is unusual when the logarithm of its amount, ln(1 + amount), is above Q3 + 1.5 × (Q3 − Q1) of its group's, the quartiles
/// taken by linear interpolation between the sorted values (position (n − 1) × p, as Excel's QUARTILE.INC and NumPy's default do).
/// The logarithm makes "unusual" a ratio rather than a difference: spending is right-skewed (many small amounts, a few large ones),
/// and on the raw amounts the fence flagged 4.8 % of the owner's 8,649 records (416), mostly ordinary taxes and savings; on the
/// logarithm it flags 1.9 % (164). Only "large" is judged:
/// spending less than usual needs no attention. A group is one sub-class (its main class follows from it) in one currency label —
/// labels are free text and never converted, so 10 USD and 10,000 KRW are never compared. A group of fewer than
/// <see cref="MinimumGroupSize"/> records has no "usual" yet and flags nothing: with three meals the quartiles are noise.
/// </para>
/// <para>
/// When the usual amounts are all the same (a fixed rent), the spread is zero and every amount above that one is flagged — a
/// changed rent is exactly what the user would want to notice.
/// </para>
/// </summary>
public static class ExpenditureOutlierPolicy
{
    /// <summary>The fewest records a sub-class + currency group needs before any of them can be judged unusual.</summary>
    public const int MinimumGroupSize = 8;

    /// <summary>How many spreads (Q3 − Q1) above Q3 the fence lies: the box-plot rule's 1.5.</summary>
    private const double FenceMultiplier = 1.5;

    /// <summary>The ids of the expenditures in <paramref name="history"/> whose amount is above their group's upper fence.</summary>
    public static IReadOnlySet<long> UnusuallyLarge(IEnumerable<Expenditure> history) =>
        history
            .GroupBy(e => (e.SubClass, e.Amount.Currency.Value))
            .Where(group => group.Count() >= MinimumGroupSize)
            .SelectMany(group =>
            {
                List<double> sorted = [.. group.Select(e => LogOf(e.Amount.Amount)).Order()];
                double q1 = Quartile(sorted, 0.25);
                double q3 = Quartile(sorted, 0.75);
                double fence = q3 + (FenceMultiplier * (q3 - q1));
                return group.Where(e => LogOf(e.Amount.Amount) > fence).Select(e => e.Id);
            })
            .ToHashSet();

    /// <summary>ln(1 + <paramref name="amount"/>): defined for a zero amount (stored amounts are never negative).</summary>
    private static double LogOf(decimal amount) => Math.Log(1 + (double)amount);

    /// <summary>The <paramref name="p"/>-quantile of <paramref name="sorted"/> by linear interpolation at position (n − 1) × p.</summary>
    private static double Quartile(List<double> sorted, double p)
    {
        double position = (sorted.Count - 1) * p;
        int lower = (int)Math.Floor(position);
        double fraction = position - lower;
        return sorted[lower] + (fraction * (sorted[lower + 1] - sorted[lower]));
    }
}
