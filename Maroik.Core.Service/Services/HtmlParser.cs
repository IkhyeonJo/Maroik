using HtmlAgilityPack;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>HtmlAgilityPack-backed implementation of <see cref="IHtmlParser"/>.</summary>
public class HtmlParser : IHtmlParser
{
    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> TransformImageAttributesAsync(
        string html,
        Func<string, CancellationToken, Task<HtmlImgPatch?>> patchFactory,
        CancellationToken ct = default)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        bool hasImages = false;

        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img"))
        {
            hasImages = true;
            string alt = imgTag.GetAttributeValue("alt", "");
            HtmlImgPatch? patch = await patchFactory(alt, ct);
            if (patch == null) continue;
            if (patch.NewAlt != null) imgTag.SetAttributeValue("alt", patch.NewAlt);
            if (patch.DataFile != null) imgTag.SetAttributeValue("data-file", patch.DataFile);
            if (patch.DataContentType != null) imgTag.SetAttributeValue("data-contenttype", patch.DataContentType);
        }

        return (doc.DocumentNode.OuterHtml, hasImages);
    }

    /// <inheritdoc />
    public string TransformImageAttributes(string html, Func<string, HtmlImgPatch?> patchFactory)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img"))
        {
            string alt = imgTag.GetAttributeValue("alt", "");
            HtmlImgPatch? patch = patchFactory(alt);
            if (patch?.NewAlt != null) imgTag.SetAttributeValue("alt", patch.NewAlt);
        }

        return doc.DocumentNode.OuterHtml;
    }
}
