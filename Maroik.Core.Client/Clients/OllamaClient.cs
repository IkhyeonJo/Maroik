using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// <see cref="ILlmClient"/> over Ollama's <c>/api/chat</c> (the <c>Maroik.Ollama</c> container on the internal network; no authentication,
/// the network is its boundary). The answer is streamed as newline-delimited JSON, one piece per line, so the page shows it as it is
/// written. A failure is not logged here: it is thrown, naming Ollama's reason, to the service that handles it and logs it once.
/// </summary>
/// <param name="httpClientFactory">Creates the HTTP client of each request.</param>
/// <param name="settings"><see cref="ServerSetting.AIBaseUrl"/> and <see cref="ServerSetting.AIModel"/>.</param>
public class OllamaClient(IHttpClientFactory httpClientFactory, IOptions<ServerSetting> settings) : ILlmClient
{
    /// <summary>
    /// How long one request may take until Ollama starts answering. A model on the server's CPU first loads (tens of seconds after a pause)
    /// and then reads the whole prompt, tools included, before the first piece of the answer — well past HttpClient's default 100 s.
    /// </summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async IAsyncEnumerable<LlmChunk> ChatAsync(LlmChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string baseUrl = settings.Value.AIBaseUrl?.Trim() ?? "";
        if (baseUrl.Length == 0)
            throw new HttpRequestException("The AI server is not configured (AI:BaseUrl).");

        using HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = RequestTimeout;
        Uri url = new(new Uri(baseUrl.EndsWith('/', StringComparison.Ordinal) ? baseUrl : baseUrl + "/"), "api/chat");

        using HttpRequestMessage message = new(HttpMethod.Post, url);
        message.Content = JsonContent.Create(ToBody(request));
        using HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Ollama at {url} answered {(int)response.StatusCode}: {ReadError(body) ?? body}", null, response.StatusCode);
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
        using StreamReader reader = new(stream);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using JsonDocument doc = JsonDocument.Parse(line);
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("error", out JsonElement error))
                throw new HttpRequestException($"Ollama at {url} failed: {error.GetString()}");
            if (root.TryGetProperty("message", out JsonElement reply))
                yield return ToChunk(reply);
        }
    }

    /// <summary>The <c>/api/chat</c> body: the model, the messages, the tools as functions, streaming on and temperature 0 (the same question gets the same tool calls).</summary>
    private JsonObject ToBody(LlmChatRequest request) => new()
    {
        ["model"] = settings.Value.AIModel,
        ["stream"] = true,
        ["options"] = new JsonObject { ["temperature"] = 0 },
        ["messages"] = new JsonArray([.. request.Messages.Select(ToMessage)]),
        ["tools"] = new JsonArray([.. request.Tools.Select(JsonNode (tool) => new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = JsonNode.Parse(tool.ParametersJsonSchema),
            },
        })]),
    };

    /// <summary>One message; an assistant's tool calls carry their arguments as an object, a tool answer the name of its tool.</summary>
    private static JsonNode ToMessage(LlmMessage message)
    {
        JsonObject node = new() { ["role"] = message.Role, ["content"] = message.Content };
        if (message.ToolCalls.Count > 0)
        {
            node["tool_calls"] = new JsonArray([.. message.ToolCalls.Select(JsonNode (call) => new JsonObject
            {
                ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = ArgumentsObject(call.ArgumentsJson) },
            })]);
        }
        if (message.ToolName is not null)
            node["tool_name"] = message.ToolName;
        return node;
    }

    /// <summary>The arguments the model wrote, as an object; anything that is not a JSON object is sent back as an empty one.</summary>
    private static JsonObject ArgumentsObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            // Expected: the model wrote malformed arguments. The tool already answered it with an error; the echo only has to be valid.
            return [];
        }
    }

    /// <summary>One streamed piece: its text and its tool calls (arguments as a JSON object string, whether Ollama sent an object or a string).</summary>
    private static LlmChunk ToChunk(JsonElement reply)
    {
        string content = reply.TryGetProperty("content", out JsonElement text) ? text.GetString() ?? "" : "";
        List<LlmToolCall> calls = [];
        if (reply.TryGetProperty("tool_calls", out JsonElement toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement call in toolCalls.EnumerateArray())
            {
                if (!call.TryGetProperty("function", out JsonElement function))
                    continue;
                string name = function.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "";
                string arguments = !function.TryGetProperty("arguments", out JsonElement a) ? "{}"
                    : a.ValueKind == JsonValueKind.String ? a.GetString() ?? "{}"
                    : a.GetRawText();
                calls.Add(new LlmToolCall(name, arguments));
            }
        }
        return new LlmChunk(content, calls);
    }

    /// <summary>The <c>error</c> of an Ollama error body, or null when the body is not one.</summary>
    private static string? ReadError(string body)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            return doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out JsonElement error)
                ? error.GetString()
                : null;
        }
        catch (JsonException)
        {
            // Expected: a proxy or a crashed server answered with plain text; the caller then shows the body itself.
            return null;
        }
    }
}
