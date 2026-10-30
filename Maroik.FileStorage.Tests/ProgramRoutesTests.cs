using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maroik.FileStorage.Tests;

/// <summary>
/// The service exposes its API controllers and nothing else: no Swagger UI and no OpenAPI document (Swashbuckle was removed on
/// 2026-10-30 at the owner's request).
/// </summary>
[Collection("FileStorage host")]
public class ProgramRoutesTests(WebApplicationFactory<Program> baseFactory) : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>The paths Swagger UI and its OpenAPI document used to answer on are not found.</summary>
    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task TheSwaggerPaths_AreNotFound(string path)
    {
        using HttpClient client = baseFactory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
