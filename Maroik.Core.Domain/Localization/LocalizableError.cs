using ErrorOr;

namespace Maroik.Core.Domain.Localization;

/// <summary>
/// Builds a validation <see cref="Error"/> whose <see cref="Error.Description"/> embeds a runtime
/// value (a bad time-zone ID, email address, hex color, class name, ...) while still carrying the
/// composite-format resource template and its arguments separately in <see cref="Error.Metadata"/>.
/// <see cref="Maroik.Core.Contract.Dtos.ServiceResult.FromError"/> reads that metadata back out so
/// the UI gets a static, resx-localizable template + args instead of an already-baked English
/// sentence it can never match against a resource key.
/// <see cref="Error.Description"/> itself is unaffected — it stays the fully-formatted message, for
/// logs and any caller that does not localize.
/// </summary>
public static class LocalizableError
{
    /// <summary><see cref="Error.Metadata"/> key holding the composite-format template (e.g. <c>"'{0}' is not valid."</c>).</summary>
    public const string ResourceKeyMetadataKey = "ResourceKey";

    /// <summary><see cref="Error.Metadata"/> key holding the <c>object[]</c> arguments for <see cref="ResourceKeyMetadataKey"/>.</summary>
    public const string ResourceArgsMetadataKey = "ResourceArgs";

    /// <summary>
    /// Creates a <see cref="ErrorType.Validation"/> error from a composite-format
    /// <paramref name="resourceTemplate"/> (e.g. <c>"'{0}' is not a recognised time-zone ID."</c>)
    /// and its <paramref name="args"/>. <see cref="Error.Description"/> is the template with
    /// <paramref name="args"/> already substituted in.
    /// </summary>
    public static Error Validation(string code, string resourceTemplate, params object[] args) =>
        Error.Validation(code, string.Format(resourceTemplate, args), new Dictionary<string, object>
        {
            [ResourceKeyMetadataKey] = resourceTemplate,
            [ResourceArgsMetadataKey] = args,
        });
}
