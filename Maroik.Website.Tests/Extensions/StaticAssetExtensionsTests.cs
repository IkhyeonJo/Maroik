using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Integration tests for <see cref="Maroik.Website.Extensions.StaticAssetExtensions.MapStaticAssets"/> —
/// verifies the explicit .js/.css MIME mappings and the X-Content-Type-Options: nosniff
/// security header applied to every static-file response.
/// </summary>
[Collection("Website Integration")]
public class StaticAssetExtensionsTests
{
    private readonly HttpClient _client;

    public StaticAssetExtensionsTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    [Fact]
    public async Task StaticFile_JavaScript_HasExplicitApplicationJavascriptContentType()
    {
        var response = await _client.GetAsync("/anonymous/dist/js/demo.js", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task StaticFile_Css_HasExplicitTextCssContentType()
    {
        var response = await _client.GetAsync("/anonymous/css/site.css", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task StaticFile_Response_IncludesNoSniffHeader()
    {
        var response = await _client.GetAsync("/anonymous/css/site.css", TestContext.Current.CancellationToken);

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Contains("nosniff", values!);
    }

    [Fact]
    public async Task StaticFile_UnknownPath_Returns404()
    {
        var response = await _client.GetAsync("/anonymous/does-not-exist.xyz", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
