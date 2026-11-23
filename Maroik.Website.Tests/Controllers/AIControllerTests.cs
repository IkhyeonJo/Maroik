using System.Net;
using System.Text.Json;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.PostgreSQL.Contexts;
using Maroik.Website.Controllers;
using Maroik.Website.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for <c>AIController</c> against a real PostgreSQL database, with the Ollama server replaced by
/// <see cref="FakeLlmClient"/>: the chat page reached through the menu (<c>/AI/UserIndex</c>, User role only), shown only to an
/// account an administrator allowed, and <c>/AI/Ask</c>, which streams the answer as one JSON object per line — localized tool
/// notices, the answer's text, and a localized error as the last line when it fails.
/// </summary>
[Collection("Website Integration")]
public class AIControllerTests(MaroikWebApplicationFactory factory)
{
    /// <summary>A question that does not bind: an array where a string belongs.</summary>
    private static readonly int[] _notAString = [1, 2];

    /// <summary>The model the host under test talks to.</summary>
    private readonly FakeLlmClient _llm = new();

    /// <summary>A host whose model is <see cref="_llm"/>.</summary>
    private WebApplicationFactory<Program> Host(Action<IServiceCollection>? more = null) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILlmClient>(_llm);
            more?.Invoke(services);
        }));

    /// <summary>A client of <paramref name="host"/> that does not follow redirects.</summary>
    private static HttpClient ClientOf(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://www.localhost/"), AllowAutoRedirect = false, HandleCookies = false });

    /// <summary>Signs in as a new User-role account, allowed the AI chat or not.</summary>
    private static async Task<AuthenticatedSession> LoginAsync(WebApplicationFactory<Program> host, HttpClient client, bool aiAllowed,
        [System.Runtime.CompilerServices.CallerMemberName] string test = "")
    {
        string email = $"ai-{test[..Math.Min(test.Length, 40)].ToLowerInvariant()}-{Guid.NewGuid():N}@test.com";
        AuthenticatedSession session = await AuthenticatedSessionHelper.LoginAsync(host, client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Accounts.Single(a => a.Email == email).AIAllowed = aiAllowed;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return session;
    }

    /// <summary>GETs <paramref name="url"/> with the session cookie.</summary>
    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, AuthenticatedSession session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", session.CookieHeader);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Asks <paramref name="body"/> and returns the response's lines, each parsed.</summary>
    private static async Task<(HttpResponseMessage Response, List<JsonElement> Lines)> AskAsync(HttpClient client, AuthenticatedSession session, object body, string? culture = null)
    {
        using var request = session.BuildJsonPostRequest("/AI/Ask", body);
        if (culture != null) request.Headers.AcceptLanguage.ParseAdd(culture);
        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        List<JsonElement> lines = [];
        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using JsonDocument doc = JsonDocument.Parse(line);
            lines.Add(doc.RootElement.Clone());
        }
        return (response, lines);
    }

    /// <summary>The type and text of each line.</summary>
    private static (string? Type, string? Text)[] Read(List<JsonElement> lines) =>
        [.. lines.Select(l => (l.GetProperty("type").GetString(), l.GetProperty("text").GetString()))];

    /// <summary>An allowed account sees the chat form, the question limit from the domain's policy and the page script; the menu links the page.</summary>
    [Fact]
    public async Task UserIndex_ShowsTheChat_ToAnAllowedAccount()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);

        HttpResponseMessage response = await GetAsync(client, "/AI/UserIndex", session);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"formAIChat\"", body, StringComparison.Ordinal);
        Assert.Contains("maxlength=\"500\"", body, StringComparison.Ordinal);
        Assert.Contains("/user/custom/AI/UserIndex/js/site.js", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/AI/UserIndex\"", body, StringComparison.Ordinal);
    }

    /// <summary>An account the administrator has not allowed is told to ask the administrator; it gets no form and no script.</summary>
    [Fact]
    public async Task UserIndex_TellsANotAllowedAccountToAskTheAdministrator()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: false);

        string body = await (await GetAsync(client, "/AI/UserIndex", session)).Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("The AI chat is not turned on for your account. Please ask the administrator.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"formAIChat\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("/user/custom/AI/UserIndex/js/site.js", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// The answer streams as newline-delimited JSON: a localized notice for each tool the model runs, then the answer's pieces. The model
    /// got the question and the earlier turns the page sent.
    /// </summary>
    [Fact]
    public async Task Ask_StreamsTheToolNoticesAndTheAnswer()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);
        _llm.Answers(new LlmChunk("", [new LlmToolCall("get_assets", "{}")]))
            .Answers(new LlmChunk("You have ", []), new LlmChunk("no assets.", []));

        (HttpResponseMessage response, List<JsonElement> lines) = await AskAsync(client, session, new
        {
            Question = "How much do I have?",
            History = new[] { new { Role = "user", Content = "hi" }, new { Role = "assistant", Content = "hello" } },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-ndjson", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal([("tool", "Looking up your assets"), ("text", "You have "), ("text", "no assets.")], Read(lines));
        Assert.Equal(["system", "user", "assistant", "user"], _llm.Requests[0].Messages.Select(m => m.Role));
        Assert.Equal("How much do I have?", _llm.Requests[0].Messages[^1].Content);
    }

    /// <summary>The tool notices and the errors follow the page's culture.</summary>
    [Fact]
    public async Task Ask_LocalizesTheNotices()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);
        _llm.Answers(new LlmChunk("", [new LlmToolCall("get_expenditures", "{}")])).Answers(new LlmChunk("없어요.", []));

        (_, List<JsonElement> lines) = await AskAsync(client, session, new { Question = "이번 달 지출?" }, "ko-KR");

        Assert.Equal(("tool", "지출 내역을 확인하고 있어요"), Read(lines)[0]);
    }

    /// <summary>An account that is not allowed gets the localized refusal as its one line; the model is not asked.</summary>
    [Fact]
    public async Task Ask_RefusesANotAllowedAccount()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: false);

        (_, List<JsonElement> lines) = await AskAsync(client, session, new { Question = "hi" }, "ko-KR");

        Assert.Equal([("error", "이 계정은 AI 대화가 켜져 있지 않아요. 관리자에게 요청해 주세요.")], Read(lines));
        Assert.Empty(_llm.Requests);
    }

    /// <summary>A blank or too long question is refused with the domain's reason, localized.</summary>
    [Fact]
    public async Task Ask_RefusesAnInvalidQuestion()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);

        (_, List<JsonElement> blank) = await AskAsync(client, session, new { Question = " " });
        (_, List<JsonElement> tooLong) = await AskAsync(client, session, new { Question = new string('a', 501) }, "ko-KR");

        Assert.Equal([("error", "Please enter a question.")], Read(blank));
        Assert.Equal([("error", "질문은 500자까지 쓸 수 있어요.")], Read(tooLong));
    }

    /// <summary>A model server that cannot be reached ends the stream with the localized "not available" line.</summary>
    [Fact]
    public async Task Ask_EndsWithNotAvailable_WhenTheModelServerIsDown()
    {
        _llm.Failure = new HttpRequestException("connection refused");
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);

        (HttpResponseMessage response, List<JsonElement> lines) = await AskAsync(client, session, new { Question = "hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([("error", "The AI is not available right now. Please try again later.")], Read(lines));
    }

    /// <summary>
    /// The failure contract of <c>/AI/Ask</c>: a service that throws becomes HTTP 200 with the generic message as the last line, never the
    /// exception's text, and the failure is logged at Error with the exception and the signed-in account.
    /// </summary>
    [Fact]
    public async Task Ask_AServiceFailure_EndsWithTheGenericMessage_AndIsLogged()
    {
        var logger = new FakeLogger<AIController>();
        var failure = new InvalidOperationException("SECRET database detail");
        var service = new Mock<IAIChatService>();
        service.Setup(s => s.AskAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<AIChatTurnRequest>>(), It.IsAny<CancellationToken>()))
            .Throws(failure);
        await using var host = Host(services =>
        {
            services.AddSingleton(service.Object);
            services.AddSingleton<ILogger<AIController>>(logger);
        });
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);

        (HttpResponseMessage response, List<JsonElement> lines) = await AskAsync(client, session, new { Question = "hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([("error", ServiceErrorKeys.TemporaryError)], Read(lines));
        FakeLogRecord log = Assert.Single(logger.Collector.GetSnapshot(), l => l.Level == LogLevel.Error);
        Assert.Same(failure, log.Exception);
        Assert.Contains("@test.com", log.Message, StringComparison.Ordinal);
    }

    /// <summary>A body that does not bind (a question that is not a string) answers the localized "Input is invalid" line.</summary>
    [Fact]
    public async Task Ask_AnswersInputIsInvalid_ForABodyThatDoesNotBind()
    {
        await using var host = Host();
        using HttpClient client = ClientOf(host);
        AuthenticatedSession session = await LoginAsync(host, client, aiAllowed: true);

        (_, List<JsonElement> lines) = await AskAsync(client, session, new { Question = _notAString });

        Assert.Equal([("error", "Input is invalid")], Read(lines));
        Assert.Empty(_llm.Requests);
    }
}
