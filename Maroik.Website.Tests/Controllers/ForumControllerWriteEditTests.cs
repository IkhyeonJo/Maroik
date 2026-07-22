using Maroik.Core.Contract.Constants;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for <c>WriteFreeBoard</c>/<c>EditFreeBoard</c> on <c>ForumController</c> —
/// the multipart-form counterparts to the JSON-bodied actions covered in
/// <c>ForumControllerBoardTests</c>. <c>BoardService.WriteBoardAsync</c>/<c>EditBoardAsync</c>
/// wrap their entire body in an <c>IUnitOfWork</c> transaction, which the EF Core InMemory
/// provider used by this test factory cannot execute, so only the ModelState validation branch
/// (which returns before the service is ever called) is exercised here — see
/// <c>AccountBookControllerAssetTests</c> for the same limitation on a simpler action.
/// </summary>
[Collection("Website Integration")]
public class ForumControllerWriteEditTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ForumControllerWriteEditTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "forum-writeedit-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private static MultipartFormDataContent BoardForm(string? title, string content = "Body", bool locked = false, bool noticed = false)
    {
        var form = new MultipartFormDataContent();
        if (title != null) form.Add(new StringContent(title), "Title");
        form.Add(new StringContent(content), "Content");
        form.Add(new StringContent(locked.ToString()), "Locked");
        form.Add(new StringContent(noticed.ToString()), "Noticed");
        return form;
    }

    [Fact]
    public async Task WriteFreeBoard_AnonymousSession_IsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Forum/WriteFreeBoard") { Content = BoardForm("Hello") };

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WriteFreeBoard_MissingTitle_ReturnsInputInvalid()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildFormPostRequest("/Forum/WriteFreeBoard", BoardForm(null));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task WriteFreeBoard_TitleTooLong_ReturnsInputInvalid()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildFormPostRequest("/Forum/WriteFreeBoard", BoardForm(new string('a', 101)));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task EditFreeBoard_MissingTitle_ReturnsInputInvalid()
    {
        var session = await LoginAsUserAsync();
        var form = BoardForm(null);
        form.Add(new StringContent("1"), "Id");
        using var request = session.BuildFormPostRequest("/Forum/EditFreeBoard", form);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }
}
