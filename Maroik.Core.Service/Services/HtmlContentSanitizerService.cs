using Ganss.Xss;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>Ganss.Xss-backed implementation of <see cref="IHtmlContentSanitizerService"/>.</summary>
public class HtmlContentSanitizerService : IHtmlContentSanitizerService
{
    /// <inheritdoc />
    public string Sanitize(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        return sanitizer.Sanitize(html);
    }
}
