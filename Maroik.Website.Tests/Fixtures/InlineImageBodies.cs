using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>Bodies of inline images as the editor submits them: each <c>alt</c> carries a storage path protected by the host.</summary>
internal static class InlineImageBodies
{
    /// <summary>
    /// A body of <paramref name="count"/> images. The paths name no stored file: a write only counts the images, it
    /// fetches none.
    /// </summary>
    public static string Of(MaroikWebApplicationFactory factory, int count)
    {
        using var scope = factory.Services.CreateScope();
        var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionService>();
        return string.Concat(Enumerable.Range(0, count).Select(i =>
            $"<img alt=\"{protector.Protect(ProtectedValue.StoredFilePath, $"upload/Forum/FreeForum/summernote/images/{i}.png")}\">"));
    }
}
