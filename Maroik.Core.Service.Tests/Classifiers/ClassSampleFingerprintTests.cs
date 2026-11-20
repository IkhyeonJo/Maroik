using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;

namespace Maroik.Core.Service.Tests.Classifiers;

/// <summary>
/// Unit tests for <see cref="ClassSampleFingerprint"/>: the fingerprint that says whether a stored model was trained on the account's
/// current records. It reads what the model reads — each record's content and class, in order — and nothing else.
/// </summary>
public class ClassSampleFingerprintTests
{
    /// <summary>A sample of <paramref name="text"/> in consumer spending / <paramref name="subClass"/>.</summary>
    private static ClassSample Sample(string text, string subClass) => new(text, new ClassSuggestion("ConsumerSpending", subClass));

    /// <summary>The same records give the same fingerprint: a SHA-256 in upper-case hex.</summary>
    [Fact]
    public void Of_TheSameRecords_IsTheSame()
    {
        string first = ClassSampleFingerprint.Of([Sample("지하철", "TransportationCost")]);

        Assert.Equal(first, ClassSampleFingerprint.Of([Sample("지하철", "TransportationCost")]));
        Assert.Matches("^[0-9A-F]{64}$", first);
    }

    /// <summary>A re-worded, re-classified, added or reordered record changes it.</summary>
    [Fact]
    public void Of_AnyChangeTheModelWouldSee_ChangesIt()
    {
        List<ClassSample> records = [Sample("지하철", "TransportationCost"), Sample("스타벅스", "MealOrEatOutExpenses")];
        string original = ClassSampleFingerprint.Of(records);

        Assert.NotEqual(original, ClassSampleFingerprint.Of([Sample("지하철 2호선", "TransportationCost"), records[1]]));
        Assert.NotEqual(original, ClassSampleFingerprint.Of([records[0], Sample("스타벅스", "LeisureOrCulture")]));
        Assert.NotEqual(original, ClassSampleFingerprint.Of([.. records, Sample("버스", "TransportationCost")]));
        Assert.NotEqual(original, ClassSampleFingerprint.Of([records[1], records[0]]));
    }

    /// <summary>The separators keep a boundary from moving unnoticed: "ab"+"c" is not "a"+"bc".</summary>
    [Fact]
    public void Of_ABoundaryMovedBetweenRecords_ChangesIt()
    {
        Assert.NotEqual(
            ClassSampleFingerprint.Of([Sample("ab", "X"), Sample("c", "X")]),
            ClassSampleFingerprint.Of([Sample("a", "X"), Sample("bc", "X")]));
    }
}
