using Maroik.Core.Domain.Finance;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Extensions;

/// <summary>
/// Renders the &lt;option&gt; list for an expenditure "sub class" &lt;select&gt; from
/// <see cref="ExpenditureClassPolicy.SubClassesByMainClass"/> (the domain policy backing
/// <see cref="ExpenditureClassPolicy.Validate"/>) instead of hardcoding it, so the list can't drift
/// out of sync with what the server actually accepts. Only the default main class's (first key's)
/// sub-classes start visible/selected; the page's own <c>ChangeCreate*MainClass</c>/
/// <c>ChangeEdit*MainClass</c> (site.ts) reveal the rest when the user picks a different main class.
/// Shared by the create/edit forms in both <c>Views/AccountBook/Expenditure.cshtml</c> and
/// <c>Views/Notice/FixedExpenditure.cshtml</c>, which otherwise repeated this identical
/// default-computation + rendering loop four times. Takes the calling view's own
/// <paramref name="localizer"/> (rather than resolving one itself) so each page's sub-class labels
/// keep resolving from that page's own resource file, exactly as before this was extracted.
/// </summary>
public static class ExpenditureSubClassOptionsExtensions
{
    public static IHtmlContent ExpenditureSubClassOptions(this IHtmlHelper html, IViewLocalizer localizer)
    {
        string defaultMainClass = ExpenditureClassPolicy.SubClassesByMainClass.Keys.First();
        string defaultSubClass = ExpenditureClassPolicy.SubClassesByMainClass[defaultMainClass][0];

        var content = new HtmlContentBuilder();
        foreach ((string mainClass, IReadOnlyList<string> subClasses) in ExpenditureClassPolicy.SubClassesByMainClass)
        {
            foreach (string subClass in subClasses)
            {
                var option = new TagBuilder("option");
                option.Attributes["value"] = subClass;
                if (mainClass != defaultMainClass)
                    option.Attributes["hidden"] = "hidden";
                if (subClass == defaultSubClass)
                    option.Attributes["selected"] = "selected";
                option.InnerHtml.Append(localizer[subClass].Value);
                content.AppendHtml(option);
            }
        }
        return content;
    }
}
