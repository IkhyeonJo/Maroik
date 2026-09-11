using ErrorOr;
using Maroik.Core.Domain.Localization;

namespace Maroik.Core.DomainTests.Localization;

/// <summary>Unit tests for <see cref="LocalizableError"/>.</summary>
public class LocalizableErrorTests
{
    /// <summary>
    /// <see cref="Error.Description"/> is the template with the args already substituted in --
    /// unchanged from what a plain <c>Error.Validation(code, $"...")</c> call would have produced --
    /// so logs and any non-localizing caller still see a normal, readable message.
    /// </summary>
    [Fact]
    public void Validation_DescriptionIsTheFormattedMessage()
    {
        Error error = LocalizableError.Validation("TimeZoneId.Invalid", "'{0}' is not a recognised time-zone ID.", "bogus/zone");

        Assert.Equal("TimeZoneId.Invalid", error.Code);
        Assert.Equal("'bogus/zone' is not a recognised time-zone ID.", error.Description);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    /// <summary>The unformatted template and the original args survive separately in Metadata.</summary>
    [Fact]
    public void Validation_MetadataCarriesTemplateAndArgsSeparately()
    {
        Error error = LocalizableError.Validation("FixedIncome.DepositDay", "Deposit day must be between 1 and {0} for month {1}.", 28, 2);

        Assert.NotNull(error.Metadata);
        Assert.Equal("Deposit day must be between 1 and {0} for month {1}.", error.Metadata![LocalizableError.ResourceKeyMetadataKey]);
        Assert.Equal([28, 2], (object[])error.Metadata[LocalizableError.ResourceArgsMetadataKey]);
    }

    /// <summary>A template with no placeholders (no args supplied) still round-trips cleanly.</summary>
    [Fact]
    public void Validation_WithNoArgs_LeavesTemplateUnformatted()
    {
        Error error = LocalizableError.Validation("Some.Code", "A static message.");

        Assert.Equal("A static message.", error.Description);
        Assert.Equal("A static message.", error.Metadata![LocalizableError.ResourceKeyMetadataKey]);
        Assert.Empty((object[])error.Metadata[LocalizableError.ResourceArgsMetadataKey]);
    }
}
