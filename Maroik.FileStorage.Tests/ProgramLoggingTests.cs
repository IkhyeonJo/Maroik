using System.Globalization;
using Maroik.FileStorage.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Tests;

/// <summary>
/// What <c>Program</c> logs and where: the console is the only provider, one line per entry, at Information and above (the
/// framework's chatter at Warning), and every request leaves one entry with its method, path, status and duration (and nothing of
/// its headers or query). Logged values can come from a request (an upload path …), so a value must not be able to end the line
/// and start a forged entry.
/// </summary>
[Collection("FileStorage host")]
public class ProgramLoggingTests(WebApplicationFactory<Program> baseFactory) : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>The category of the request log written by <c>UseHttpLogging</c>.</summary>
    private const string RequestLogCategory = "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware";

    /// <summary>Renders one entry through the host's console formatter.</summary>
    private string Render(LogLevel level, string message, Exception? exception = null)
    {
        string? name = baseFactory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName;
        ConsoleFormatter formatter = baseFactory.Services.GetServices<ConsoleFormatter>().Single(f => f.Name == name);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        formatter.Write(new LogEntry<string>(level, "Test", new EventId(0), message, exception, (state, _) => state), null, writer);
        return writer.ToString();
    }

    /// <summary>The host's default providers (debug, event source …) are gone; it logs to the console alone.</summary>
    [Fact]
    public void TheHost_LogsToTheConsoleOnly()
    {
        Assert.IsType<ConsoleLoggerProvider>(Assert.Single(baseFactory.Services.GetServices<ILoggerProvider>()));
    }

    /// <summary>A request is logged once at Information with its method, path, status and duration, but not its cookies or query.</summary>
    [Fact]
    public async Task EachRequest_IsLoggedOnce_WithoutItsHeadersOrQuery()
    {
        using WebApplicationFactory<Program> host = baseFactory.WithWebHostBuilder(b => b.ConfigureTestServices(services => services.AddFakeLogging()));
        using HttpClient client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/File/download?probe=query-value-1");
        request.Content = MultipartForm.Of(("filePath", "upload/none.png"));
        request.Headers.Add("Cookie", "Probe=cookie-value-1");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        FakeLogRecord record = Assert.Single(host.Services.GetFakeLogCollector().GetSnapshot(),
            r => r.Category == RequestLogCategory && r.Message.Contains("/api/File/download", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Contains("Method: POST", record.Message, StringComparison.Ordinal);
        Assert.Contains($"StatusCode: {(int)response.StatusCode}", record.Message, StringComparison.Ordinal);
        Assert.Contains("Duration: ", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("cookie-value-1", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("query-value-1", record.Message, StringComparison.Ordinal);
    }

    /// <summary>The simple formatter writes one uncoloured line per entry, stamped with the time of day, without scopes.</summary>
    [Fact]
    public void TheConsole_UsesTheSimpleFormatter_OnOneUncolouredLine()
    {
        Assert.Equal(ConsoleFormatterNames.Simple, baseFactory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName);
        SimpleConsoleFormatterOptions options = baseFactory.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;
        Assert.True(options.SingleLine);
        Assert.False(options.IncludeScopes);
        Assert.Equal(LoggerColorBehavior.Disabled, options.ColorBehavior);
        Assert.Matches(@"^\d{2}:\d{2}:\d{2} warn: Test\[0\] Upload refused\r?\n$", Render(LogLevel.Warning, "Upload refused"));
    }

    /// <summary>A line break inside a logged value cannot end the entry's line, so it cannot start a forged entry.</summary>
    [Fact]
    public void TheConsole_KeepsAValueWithALineBreakOnTheEntrysLine()
    {
        string rendered = Render(LogLevel.Warning, "File upload refused: upload/a.png\n00:00:00 info: Maroik[0] File uploaded: upload/b.png");

        Assert.EndsWith(Environment.NewLine, rendered, StringComparison.Ordinal);
        string line = rendered[..^Environment.NewLine.Length];
        Assert.DoesNotContain('\n', line);
        Assert.EndsWith("refused: upload/a.png 00:00:00 info: Maroik[0] File uploaded: upload/b.png", line, StringComparison.Ordinal);
    }

    /// <summary>An exception (its message and stack trace) is written on the entry's line as well.</summary>
    [Fact]
    public void TheConsole_WritesTheExceptionOnTheEntrysLine()
    {
        string line = Render(LogLevel.Error, "Upload failed", new InvalidOperationException("first\nsecond"))[..^Environment.NewLine.Length];

        Assert.DoesNotContain('\n', line);
        Assert.Contains("Upload failed System.InvalidOperationException: first second", line, StringComparison.Ordinal);
    }

    /// <summary>Information and above is logged, except the framework's (Microsoft / System) chatter below Warning; the host lifetime and the request log stay at Information.</summary>
    [Theory]
    [InlineData("Maroik.FileStorage.Controllers.FileController", LogLevel.Information, true)]
    [InlineData("Maroik.FileStorage.Controllers.FileController", LogLevel.Debug, false)]
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Information, false)]
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning, true)]
    [InlineData("Microsoft.Hosting.Lifetime", LogLevel.Information, true)]
    [InlineData(RequestLogCategory, LogLevel.Information, true)]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Information, false)]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Warning, true)]
    public void TheHost_LogsInformationAndAbove_AndTheFrameworkFromWarning(string category, LogLevel level, bool enabled)
    {
        Assert.Equal(enabled, baseFactory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category).IsEnabled(level));
    }
}
