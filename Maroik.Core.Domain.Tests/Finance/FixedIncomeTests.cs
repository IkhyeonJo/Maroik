using Maroik.Core.Domain.Finance;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Finance.FixedIncome"/>.
/// Covers registration validation (deposit day/month range, main class), unpunctuality flag management, and update logic.
/// </summary>
public class FixedIncomeTests
{
    private static readonly DateTime Maturity = DateTime.UtcNow.AddYears(1);

    private static FixedIncome ValidFixedIncome() =>
        FixedIncome.Register("user@example.com", "RegularIncome", "LaborIncome", "Salary",
            3000000m, "KRW", "My Bank", 1, 25, Maturity).Value;

    // -- Register -------------------------------------------------------------

    /// <summary>Register returns fixed income, when valid.</summary>
    [Fact]
    public void Register_ReturnsFixedIncome_WhenValid()
    {
        var result = FixedIncome.Register("user@example.com", "RegularIncome", "LaborIncome", "Salary",
            3000000m, "KRW", "My Bank", 1, 25, Maturity);

        Assert.False(result.IsError);
        Assert.Equal(25, result.Value.DepositDay);
        Assert.Equal(1, result.Value.DepositMonth);
    }

    /// <summary>Register returns error, when deposit day out of range.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Register_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var result = FixedIncome.Register("user@example.com", "RegularIncome", "LaborIncome", null,
            0m, "KRW", "My Bank", 1, day, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositDay", result.FirstError.Code);
    }

    /// <summary>Register returns error, when deposit month out of range.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Register_ReturnsError_WhenDepositMonthOutOfRange(short month)
    {
        var result = FixedIncome.Register("user@example.com", "RegularIncome", "LaborIncome", null,
            0m, "KRW", "My Bank", month, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositMonth", result.FirstError.Code);
    }

    /// <summary>Register returns error, when main class empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_ReturnsError_WhenMainClassEmpty(string? mainClass)
    {
        var result = FixedIncome.Register("user@example.com", mainClass, "LaborIncome", null,
            0m, "KRW", "My Bank", 1, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.MainClassEmpty", result.FirstError.Code);
    }

    // -- MarkUnpunctual / ClearUnpunctuality -----------------------------------

    /// <summary>Mark unpunctual sets flag.</summary>
    [Fact]
    public void MarkUnpunctual_SetsFlag()
    {
        var fi = ValidFixedIncome();

        fi.MarkUnpunctual();

        Assert.True(fi.Unpunctuality);
    }

    /// <summary>Clear unpunctuality clears flag.</summary>
    [Fact]
    public void ClearUnpunctuality_ClearsFlag()
    {
        var fi = ValidFixedIncome();
        fi.MarkUnpunctual();

        fi.ClearUnpunctuality();

        Assert.False(fi.Unpunctuality);
    }

    // -- Update ----------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var fi = ValidFixedIncome();

        var result = fi.Update("RegularIncome", "PensionIncome", "Pension", 500000m, "KRW", "Pension Account", 1, 10, Maturity, "note");

        Assert.False(result.IsError);
        Assert.Equal("PensionIncome", fi.SubClass);
        Assert.Equal(10, fi.DepositDay);
    }

    /// <summary>Update returns error, when deposit day out of range.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Update_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var fi = ValidFixedIncome();

        var result = fi.Update("RegularIncome", "LaborIncome", null, 0m, "KRW", "My Bank", 1, day, Maturity, null);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositDay", result.FirstError.Code);
    }
}
