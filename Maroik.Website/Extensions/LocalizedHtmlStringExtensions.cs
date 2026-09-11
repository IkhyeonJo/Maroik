using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.Website.Extensions;

/// <summary>
/// Renders a <see cref="LocalizedHtmlString"/> to plain text, substituting its format arguments.
/// <para>
/// <see cref="LocalizedHtmlString.Value"/> is documented as "the original resource string, prior to
/// formatting with any constructor arguments" — it never substitutes <c>{0}</c>-style placeholders —
/// and its inherited <see cref="object.ToString"/> is not overridden, so it returns the CLR type name
/// rather than the rendered message. The only supported way to materialize the formatted (and
/// HTML-encoded) text is <see cref="LocalizedHtmlString.WriteTo"/>, which this wraps.
/// </para>
/// </summary>
public static class LocalizedHtmlStringExtensions
{
    /// <summary>Formats <paramref name="localized"/>'s arguments into its resource template and HTML-encodes them.</summary>
    public static string ToPlainString(this LocalizedHtmlString localized)
    {
        using var writer = new StringWriter();
        localized.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
