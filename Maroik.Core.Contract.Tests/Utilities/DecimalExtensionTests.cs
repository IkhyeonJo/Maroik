using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Utilities.DecimalExtension"/>.
/// Verifies trailing-zero trimming for <see cref="decimal"/> values
/// (e.g. <c>1000.00 → "1000"</c>) used when formatting currency amounts.
/// </summary>
public class DecimalExtensionTests
{
    // ── TrimTrailingZeros (decimal) ──────────────────────────────────────────

    [Theory]
    [InlineData(1000.00, "1000")]
    [InlineData(0.00, "0")]
    [InlineData(-500.00, "-500")]
    public void TrimTrailingZeros_FormatsWholeNumber_WithoutDecimalPoint(decimal value, string expected)
    {
        Assert.Equal(expected, value.TrimTrailingZeros());
    }

    [Theory]
    [InlineData(3.14, "3.14")]
    [InlineData(1.50, "1.5")]
    [InlineData(0.1, "0.1")]
    [InlineData(100.10, "100.1")]
    public void TrimTrailingZeros_RemovesTrailingDecimalZeros(decimal value, string expected)
    {
        Assert.Equal(expected, value.TrimTrailingZeros());
    }

    [Fact]
    public void TrimTrailingZeros_HandlesLargeWholeNumber()
    {
        decimal value = 1_000_000m;
        string result = value.TrimTrailingZeros();
        Assert.Equal("1000000", result);
    }

    // ── TrimTrailingZeros (decimal?) ─────────────────────────────────────────

    [Fact]
    public void TrimTrailingZeros_NullableTreatsNullAsZero()
    {
        decimal? value = null;
        string result = value.TrimTrailingZeros();
        Assert.Equal("0", result);
    }

    [Theory]
    [InlineData(500.00, "500")]
    [InlineData(3.14, "3.14")]
    [InlineData(2.50, "2.5")]
    public void TrimTrailingZeros_Nullable_BehavesLikeNonNullable_WhenHasValue(decimal value, string expected)
    {
        decimal? nullable = value;
        Assert.Equal(expected, nullable.TrimTrailingZeros());
    }

    [Fact]
    public void TrimTrailingZeros_Nullable_WholeNumber_DoesNotShowDecimalPoint()
    {
        decimal? value = 999m;
        Assert.Equal("999", value.TrimTrailingZeros());
    }
}
