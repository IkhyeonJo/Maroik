using HtmlAgilityPack;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>HtmlAgilityPack-backed implementation of <see cref="IHtmlParserService"/>.</summary>
public class HtmlParserService : IHtmlParserService
{
    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> TransformImageAttributesAsync(
        string html,
        Func<string, CancellationToken, Task<HtmlImgPatch?>> patchFactory,
        CancellationToken ct = default)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        List<HtmlNode> imgTags = doc.DocumentNode.Descendants("img").ToList();

        // Fetch every image's patch concurrently instead of awaiting one at a time,
        // since patchFactory typically makes a network call to the file-storage service.
        HtmlImgPatch?[] patches = await Task.WhenAll(
            imgTags.Select(imgTag => patchFactory(imgTag.GetAttributeValue("alt", ""), ct)));

        for (int i = 0; i < imgTags.Count; i++)
        {
            HtmlImgPatch? patch = patches[i];
            if (patch == null) continue;
            HtmlNode imgTag = imgTags[i];
            if (patch.NewAlt != null) imgTag.SetAttributeValue("alt", patch.NewAlt);
            if (patch.DataFile != null) imgTag.SetAttributeValue("data-file", patch.DataFile);
            if (patch.DataContentType != null) imgTag.SetAttributeValue("data-contenttype", patch.DataContentType);
        }

        return (doc.DocumentNode.OuterHtml, imgTags.Count > 0);
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
