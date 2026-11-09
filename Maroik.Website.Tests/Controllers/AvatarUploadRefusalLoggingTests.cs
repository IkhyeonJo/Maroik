using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Primitives;
using Maroik.Website.Controllers;
using Maroik.Website.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// A refused avatar upload is a security event like a refused editor image: the controller's own size and extension checks each
/// log one Warning naming the reason and the signed-in uploader. The logger is replaced by a <see cref="FakeLogger{T}"/> in one
/// derived host; the session is real.
/// </summary>
[Collection("Website Integration")]
public class AvatarUploadRefusalLoggingTests(MaroikWebApplicationFactory factory)
{
    /// <summary>Captures what <see cref="ManagementController"/> logs.</summary>
    private readonly FakeLogger<ManagementController> _logger = new();

    /// <summary>A host whose controller logger is the fake above.</summary>
    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddSingleton<ILogger<ManagementController>>(_logger)));

    /// <summary>
    /// Signs in as a fresh user on <paramref name="host"/>, posts <paramref name="bytes"/> as the avatar
    /// <paramref name="fileName"/>, asserts the upload was refused, and returns the uploader's e-mail.
    /// </summary>
    private static async Task<string> PostRefusedAvatarAsync(
        WebApplicationFactory<Program> host, string fileName, byte[] bytes, string contentType)
    {
        using HttpClient client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://www.localhost/"), AllowAutoRedirect = false, HandleCookies = false
        });
        string email = $"avatar-refusal-{Guid.NewGuid():N}@test.com";
        var session = await AuthenticatedSessionHelper.LoginAsync(host, client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

        var form = new MultipartFormDataContent();
        MultipartForm.AddFile(form, "ProfileAvatarFiles", fileName, bytes, contentType);
        using var request = session.BuildFormPostRequest("/Management/UpdateProfileAvatar", form);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json, StringComparison.Ordinal);
        return email;
    }

    /// <summary>Asserts exactly one Warning was logged for the refusal, naming <paramref name="reason"/> and <paramref name="email"/>.</summary>
    private void AssertRefusalLoggedOnce(string reason, string email)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level >= LogLevel.Warning);
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("Avatar upload refused", record.Message, StringComparison.Ordinal);
        Assert.Contains(reason, record.Message, StringComparison.Ordinal);
        Assert.Contains(email, record.Message, StringComparison.Ordinal);
    }

    /// <summary>A file with an extension outside jpg / jpeg / png is refused by the controller, and logged.</summary>
    [Fact]
    public async Task UpdateProfileAvatar_LogsARefusedExtension()
    {
        await using var host = CreateHost();

        string email = await PostRefusedAvatarAsync(host, "notes.txt", [1, 2, 3, 4], "text/plain");

        AssertRefusalLoggedOnce(".txt", email);
    }

    /// <summary>A file over the configured size limit is refused by the controller, and logged with its size.</summary>
    [Fact]
    public async Task UpdateProfileAvatar_LogsARefusedOversizedFile()
    {
        await using var host = CreateHost();
        long maxBytes = host.Services.GetRequiredService<IOptions<ServerSetting>>().Value.MaxAttachedFileSizeBytes;

        string email = await PostRefusedAvatarAsync(host, "huge.png", new byte[maxBytes + 1], "image/png");

        AssertRefusalLoggedOnce($"{maxBytes + 1}", email);
    }
}
