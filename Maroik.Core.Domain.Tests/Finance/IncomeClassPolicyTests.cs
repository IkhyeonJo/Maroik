using Maroik.Core.Domain.Finance;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="IncomeClassPolicy"/>.
/// Covers all valid main/sub-class combinations, invalid sub-classes per main class,
/// and unknown main classes including null and empty inputs.
/// </summary>
public class IncomeClassPolicyTests
{
    // -- RegularIncome ---------------------------------------------------------

    [Theory]
    [InlineData("LaborIncome")]
    [InlineData("BusinessIncome")]
    [InlineData("PensionIncome")]
    [InlineData("FinancialIncome")]
    [InlineData("RentalIncome")]
    [InlineData("OtherIncome")]
    public void Validate_ReturnsSuccess_ForAllRegularIncomeSubClasses(string subClass)
    {
        var result = IncomeClassPolicy.Validate("RegularIncome", subClass);

        Assert.False(result.IsError);
    }

    [Theory]
    [InlineData("UnknownSub")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenRegularIncomeSubClassInvalid(string? subClass)
    {
        var result = IncomeClassPolicy.Validate("RegularIncome", subClass);

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }

    // -- IrregularIncome -------------------------------------------------------

    [Theory]
    [InlineData("LaborIncome")]
    [InlineData("OtherIncome")]
    public void Validate_ReturnsSuccess_ForAllIrregularIncomeSubClasses(string subClass)
    {
        var result = IncomeClassPolicy.Validate("IrregularIncome", subClass);

        Assert.False(result.IsError);
    }

    [Theory]
    [InlineData("BusinessIncome")]
    [InlineData("PensionIncome")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenIrregularIncomeSubClassInvalid(string? subClass)
    {
        var result = IncomeClassPolicy.Validate("IrregularIncome", subClass);

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }

    // -- Unknown main class ----------------------------------------------------

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenMainClassUnknown(string? mainClass)
    {
        var result = IncomeClassPolicy.Validate(mainClass, "LaborIncome");

        Assert.True(result.IsError);
        Assert.Equal("IncomeClass.Invalid", result.FirstError.Code);
    }
}
