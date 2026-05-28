using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Tests.Domains.Finance;

/// <summary>
/// Unit tests for <see cref="ExpenditureClassPolicy"/>.
/// Covers all valid main/sub-class combinations (with and without deposit-asset requirement),
/// invalid sub-classes per main class, and unknown main classes including null and empty inputs.
/// </summary>
public class ExpenditureClassPolicyTests
{
    // -- RegularSavings --------------------------------------------------------

    [Theory]
    [InlineData("Deposit")]
    [InlineData("Investment")]
    public void Validate_ReturnsTrue_ForRegularSavingsSubClasses(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("RegularSavings", subClass);

        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenRegularSavingsSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("RegularSavings", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- NonConsumerSpending ---------------------------------------------------

    [Theory]
    [InlineData("PublicPension")]
    [InlineData("DebtRepayment")]
    public void Validate_ReturnsTrue_ForNonConsumerSpendingWithDepositAsset(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    [Theory]
    [InlineData("Tax")]
    [InlineData("SocialInsurance")]
    [InlineData("InterHouseholdTranserExpenses")]
    [InlineData("NonProfitOrganizationTransfer")]
    public void Validate_ReturnsFalse_ForNonConsumerSpendingWithoutDepositAsset(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenNonConsumerSpendingSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("NonConsumerSpending", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- ConsumerSpending ------------------------------------------------------

    [Theory]
    [InlineData("MealOrEatOutExpenses")]
    [InlineData("HousingOrSuppliesCost")]
    [InlineData("EducationExpenses")]
    [InlineData("MedicalExpenses")]
    [InlineData("TransportationCost")]
    [InlineData("CommunicationCost")]
    [InlineData("LeisureOrCulture")]
    [InlineData("ClothingOrShoes")]
    [InlineData("PinMoney")]
    [InlineData("ProtectionTypeInsurance")]
    [InlineData("OtherExpenses")]
    [InlineData("UnknownExpenditure")]
    public void Validate_ReturnsFalse_ForAllConsumerSpendingSubClasses(string subClass)
    {
        var result = ExpenditureClassPolicy.Validate("ConsumerSpending", subClass);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenConsumerSpendingSubClassInvalid(string? subClass)
    {
        var result = ExpenditureClassPolicy.Validate("ConsumerSpending", subClass);

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }

    // -- Unknown main class ----------------------------------------------------

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenMainClassUnknown(string? mainClass)
    {
        var result = ExpenditureClassPolicy.Validate(mainClass, "Tax");

        Assert.True(result.IsError);
        Assert.Equal("ExpenditureClass.Invalid", result.FirstError.Code);
    }
}
