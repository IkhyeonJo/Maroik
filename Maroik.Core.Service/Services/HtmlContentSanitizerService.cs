using Ganss.Xss;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>Ganss.Xss-backed implementation of <see cref="IHtmlContentSanitizerService"/>.</summary>
/// <remarks>
/// Registered as a singleton. The <see cref="HtmlSanitizer"/> is configured once at construction
/// and reused for every call: building it (and its allow-lists) per <see cref="Sanitize"/> call
/// was pure per-request allocation. <see cref="HtmlSanitizer.Sanitize(string)"/> does not mutate
/// the configured allow-lists, so concurrent calls on the shared instance are safe.
/// </remarks>
public class HtmlContentSanitizerService : IHtmlContentSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>Builds the shared, pre-configured <see cref="HtmlSanitizer"/>.</summary>
    public HtmlContentSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedAttributes.Add("class");
    }

    /// <inheritdoc />
    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
