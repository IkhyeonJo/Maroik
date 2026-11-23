using System.Net;
using System.Text;
using System.Text.Json;
using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// <see cref="OllamaClient"/> against an in-process stand-in for Ollama's <c>/api/chat</c>: the request it sends (model, messages, tool
/// calls, tools, streaming) and how it reads the newline-delimited JSON answer.
/// </summary>
public class OllamaClientTests
{
    /// <summary>An <see cref="HttpClient"/> that records whether it was disposed.</summary>
    private sealed class TrackingHttpClient(HttpMessageHandler handler) : HttpClient(handler, disposeHandler: false)
    {
        /// <summary>True once <see cref="Dispose(bool)"/> ran.</summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>Hands out clients over one handler and keeps them, like the real factory (a client does not own the handler).</summary>
    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        /// <summary>Every client created so far.</summary>
        public List<TrackingHttpClient> Created { get; } = [];

        /// <inheritdoc />
        public HttpClient CreateClient(string name)
        {
            var client = new TrackingHttpClient(handler);
            Created.Add(client);
            return client;
        }
    }

    /// <summary>Answers every request with a fixed status and body, and keeps the request's URL and body.</summary>
    private sealed class FakeOllama(HttpStatusCode status, string body) : HttpMessageHandler
    {
        /// <summary>The URL of the last request.</summary>
        public Uri? LastUri { get; private set; }

        /// <summary>The body of the last request.</summary>
        public string LastBody { get; private set; } = "";

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            LastBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/x-ndjson") };
        }
    }

    /// <summary>A client of <paramref name="handler"/> configured with a base URL and a model.</summary>
    private static (OllamaClient Client, FakeHttpClientFactory Factory) Create(HttpMessageHandler handler, string? baseUrl = "http://ollama:11434/")
    {
        var factory = new FakeHttpClientFactory(handler);
        return (new OllamaClient(factory, Options.Create(new ServerSetting { AIBaseUrl = baseUrl, AIModel = "llama3.1:8b" })), factory);
    }

    /// <summary>A request with every kind of message and one tool.</summary>
    private static LlmChatRequest SampleRequest() => new(
        [
            new LlmMessage("system", "rules"),
            new LlmMessage("user", "이번 달 지출?"),
            new LlmMessage("assistant", "", [new LlmToolCall("get_expenditures", """{"period":"this_month"}""")]),
            new LlmMessage("tool", """{"rows":[]}""", [], "get_expenditures"),
        ],
        [new LlmToolDefinition("get_expenditures", "Lists expenditures.", """{"type":"object","properties":{"period":{"type":"string"}}}""")]);

    /// <summary>Collects every chunk of a streamed answer.</summary>
    private static async Task<List<LlmChunk>> ReadAllAsync(IAsyncEnumerable<LlmChunk> chunks)
    {
        List<LlmChunk> all = [];
        await foreach (LlmChunk chunk in chunks)
            all.Add(chunk);
        return all;
    }

    /// <summary>
    /// The request goes to <c>/api/chat</c> with the configured model, streaming on, temperature 0, every message (an assistant's tool calls
    /// as argument objects, a tool answer with its tool's name) and the tools as functions with their JSON Schema.
    /// </summary>
    [Fact]
    public async Task ChatAsync_SendsTheModelTheMessagesAndTheTools()
    {
        using var ollama = new FakeOllama(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"},"done":true}""" + "\n");
        (OllamaClient client, _) = Create(ollama);

        await ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken));

        Assert.Equal("http://ollama:11434/api/chat", ollama.LastUri!.ToString());
        using JsonDocument body = JsonDocument.Parse(ollama.LastBody);
        JsonElement root = body.RootElement;
        Assert.Equal("llama3.1:8b", root.GetProperty("model").GetString());
        Assert.True(root.GetProperty("stream").GetBoolean());
        Assert.Equal(0, root.GetProperty("options").GetProperty("temperature").GetDouble());

        JsonElement[] messages = [.. root.GetProperty("messages").EnumerateArray()];
        Assert.Equal(["system", "user", "assistant", "tool"], messages.Select(m => m.GetProperty("role").GetString()));
        Assert.Equal("이번 달 지출?", messages[1].GetProperty("content").GetString());
        JsonElement call = messages[2].GetProperty("tool_calls")[0].GetProperty("function");
        Assert.Equal("get_expenditures", call.GetProperty("name").GetString());
        Assert.Equal("this_month", call.GetProperty("arguments").GetProperty("period").GetString());
        Assert.False(messages[1].TryGetProperty("tool_calls", out _));
        Assert.Equal("get_expenditures", messages[3].GetProperty("tool_name").GetString());

        JsonElement tool = root.GetProperty("tools")[0];
        Assert.Equal("function", tool.GetProperty("type").GetString());
        Assert.Equal("get_expenditures", tool.GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("Lists expenditures.", tool.GetProperty("function").GetProperty("description").GetString());
        Assert.Equal("object", tool.GetProperty("function").GetProperty("parameters").GetProperty("type").GetString());
    }

    /// <summary>A base URL without the trailing slash works the same.</summary>
    [Fact]
    public async Task ChatAsync_AcceptsABaseUrlWithoutATrailingSlash()
    {
        using var ollama = new FakeOllama(HttpStatusCode.OK, """{"message":{"content":""},"done":true}""");
        (OllamaClient client, _) = Create(ollama, "http://ollama:11434");

        await ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken));

        Assert.Equal("http://ollama:11434/api/chat", ollama.LastUri!.ToString());
    }

    /// <summary>The answer's pieces arrive in order; a piece's tool calls carry their arguments as a JSON object string; blank lines are skipped.</summary>
    [Fact]
    public async Task ChatAsync_StreamsTheTextAndTheToolCalls()
    {
        string ndjson = string.Join('\n',
            """{"message":{"role":"assistant","content":"이번 "},"done":false}""",
            "",
            """{"message":{"role":"assistant","content":"달","tool_calls":[{"function":{"name":"get_assets","arguments":{}}},{"function":{"name":"get_incomes","arguments":{"period":"last_month"}}}]},"done":false}""",
            """{"message":{"role":"assistant","content":""},"done":true}""");
        using var ollama = new FakeOllama(HttpStatusCode.OK, ndjson);
        (OllamaClient client, _) = Create(ollama);

        List<LlmChunk> chunks = await ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken));

        Assert.Equal("이번 달", string.Concat(chunks.Select(c => c.Content)));
        LlmToolCall[] calls = [.. chunks.SelectMany(c => c.ToolCalls)];
        Assert.Equal(["get_assets", "get_incomes"], calls.Select(c => c.Name));
        Assert.Equal("{}", calls[0].ArgumentsJson);
        using JsonDocument args = JsonDocument.Parse(calls[1].ArgumentsJson);
        Assert.Equal("last_month", args.RootElement.GetProperty("period").GetString());
    }

    /// <summary>A refused request throws, naming the status and Ollama's own reason.</summary>
    [Fact]
    public async Task ChatAsync_Throws_WhenOllamaRefusesTheRequest()
    {
        using var ollama = new FakeOllama(HttpStatusCode.NotFound, """{"error":"model 'llama3.1:8b' not found"}""");
        (OllamaClient client, _) = Create(ollama);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken)));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Contains("model 'llama3.1:8b' not found", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>An error reported in the middle of the stream throws with its reason.</summary>
    [Fact]
    public async Task ChatAsync_Throws_OnAnErrorLineInTheStream()
    {
        using var ollama = new FakeOllama(HttpStatusCode.OK, """{"message":{"content":"a"},"done":false}""" + "\n" + """{"error":"out of memory"}""");
        (OllamaClient client, _) = Create(ollama);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken)));

        Assert.Contains("out of memory", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>Without a configured base URL nothing is sent and the call fails like an unreachable server.</summary>
    [Fact]
    public async Task ChatAsync_Throws_WhenNoBaseUrlIsConfigured()
    {
        using var ollama = new FakeOllama(HttpStatusCode.OK, "");
        (OllamaClient client, _) = Create(ollama, " ");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken)));

        Assert.Contains("AI:BaseUrl", ex.Message, StringComparison.Ordinal);
        Assert.Null(ollama.LastUri);
    }

    /// <summary>The client it creates is disposed once the answer has been read, and waits long enough for a model on a CPU.</summary>
    [Fact]
    public async Task ChatAsync_DisposesItsClient_AndAllowsALongAnswer()
    {
        using var ollama = new FakeOllama(HttpStatusCode.OK, """{"message":{"content":""},"done":true}""");
        (OllamaClient client, FakeHttpClientFactory factory) = Create(ollama);

        await ReadAllAsync(client.ChatAsync(SampleRequest(), TestContext.Current.CancellationToken));

        TrackingHttpClient created = Assert.Single(factory.Created);
        Assert.True(created.Disposed);
        Assert.Equal(OllamaClient.RequestTimeout, created.Timeout);
        Assert.True(OllamaClient.RequestTimeout >= TimeSpan.FromMinutes(5));
    }
}
