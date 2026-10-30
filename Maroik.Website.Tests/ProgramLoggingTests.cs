using System.Globalization;
using Maroik.Website.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Tests;

/// <summary>
/// What <c>Program</c> logs and where: the console is the only provider, one line per entry, at Information and above (the
/// framework's chatter at Warning); every request leaves one entry with its method, path, status and duration (and nothing of its
/// headers or query), and running without Valkey is reported as a warning. Logged values can come from a request (an e-mail
/// address typed into the login form, a file name …), so a value must not be able to end the line and start a forged entry.
/// </summary>
[Collection("Website Integration")]
public class ProgramLoggingTests(MaroikWebApplicationFactory factory)
{
    /// <summary>The category of the request log written by <c>UseHttpLogging</c>.</summary>
    private const string RequestLogCategory = "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware";

    /// <summary>A host derived from <paramref name="factory"/> that also logs to a <see cref="FakeLogger"/> (subject to the host's filters).</summary>
    private static WebApplicationFactory<Program> WithFakeLogging(MaroikWebApplicationFactory factory) =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(services => services.AddFakeLogging()));

    /// <summary>Renders one entry through the host's console formatter.</summary>
    private string Render(LogLevel level, string message, Exception? exception = null)
    {
        string? name = factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName;
        ConsoleFormatter formatter = factory.Services.GetServices<ConsoleFormatter>().Single(f => f.Name == name);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        formatter.Write(new LogEntry<string>(level, "Test", new EventId(0), message, exception, (state, _) => state), null, writer);
        return writer.ToString();
    }

    /// <summary>The host's default providers (debug, event source …) are gone; it logs to the console alone.</summary>
    [Fact]
    public void TheHost_LogsToTheConsoleOnly()
    {
        Assert.IsType<ConsoleLoggerProvider>(Assert.Single(factory.Services.GetServices<ILoggerProvider>()));
    }

    /// <summary>A request is logged once at Information with its method, path, status and duration, but not its cookies or query.</summary>
    [Fact]
    public async Task EachRequest_IsLoggedOnce_WithoutItsHeadersOrQuery()
    {
        using WebApplicationFactory<Program> host = WithFakeLogging(factory);
        using HttpClient client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://www.localhost/"), AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Login?probe=query-value-1");
        request.Headers.Add("Cookie", "Probe=cookie-value-1");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        FakeLogRecord record = Assert.Single(host.Services.GetFakeLogCollector().GetSnapshot(),
            r => r.Category == RequestLogCategory && r.Message.Contains("/Account/Login", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Contains("Method: GET", record.Message, StringComparison.Ordinal);
        Assert.Contains($"StatusCode: {(int)response.StatusCode}", record.Message, StringComparison.Ordinal);
        Assert.Contains("Duration: ", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("cookie-value-1", record.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("query-value-1", record.Message, StringComparison.Ordinal);
    }

    /// <summary>Without a Valkey connection string the host warns that the Data Protection keys stay on this server's disk.</summary>
    [Fact]
    public void WithoutValkey_TheHostWarnsThatTheKeysStayOnDisk()
    {
        using WebApplicationFactory<Program> host = factory.WithWebHostBuilder(b => b
            .UseSetting("ConnectionStrings:Valkey", "")
            .ConfigureTestServices(services => services.AddFakeLogging()));

        FakeLogRecord record = Assert.Single(host.Services.GetFakeLogCollector().GetSnapshot(),
            r => r.Message.Contains("No Valkey connection string configured", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Contains("dataprotection-keys", record.Message, StringComparison.Ordinal);
    }

    /// <summary>The simple formatter writes one uncoloured line per entry, stamped with the time of day, without scopes.</summary>
    [Fact]
    public void TheConsole_UsesTheSimpleFormatter_OnOneUncolouredLine()
    {
        Assert.Equal(ConsoleFormatterNames.Simple, factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName);
        SimpleConsoleFormatterOptions options = factory.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;
        Assert.True(options.SingleLine);
        Assert.False(options.IncludeScopes);
        Assert.Equal(LoggerColorBehavior.Disabled, options.ColorBehavior);
        Assert.Matches(@"^\d{2}:\d{2}:\d{2} warn: Test\[0\] Login failed\r?\n$", Render(LogLevel.Warning, "Login failed"));
    }

    /// <summary>A line break inside a logged value cannot end the entry's line, so it cannot start a forged entry.</summary>
    [Fact]
    public void TheConsole_KeepsAValueWithALineBreakOnTheEntrysLine()
    {
        string rendered = Render(LogLevel.Warning, "Login failed: no account for a@b.com\n00:00:00 info: Maroik[0] Login succeeded for admin@maroik.com");

        Assert.EndsWith(Environment.NewLine, rendered, StringComparison.Ordinal);
        string line = rendered[..^Environment.NewLine.Length];
        Assert.DoesNotContain('\n', line);
        Assert.EndsWith("no account for a@b.com 00:00:00 info: Maroik[0] Login succeeded for admin@maroik.com", line, StringComparison.Ordinal);
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
    [InlineData("Maroik.Website.Controllers.AccountController", LogLevel.Information, true)]
    [InlineData("Maroik.Website.Controllers.AccountController", LogLevel.Debug, false)]
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Information, false)]
    [InlineData("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning, true)]
    [InlineData("Microsoft.Hosting.Lifetime", LogLevel.Information, true)]
    [InlineData(RequestLogCategory, LogLevel.Information, true)]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Information, false)]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Warning, true)]
    public void TheHost_LogsInformationAndAbove_AndTheFrameworkFromWarning(string category, LogLevel level, bool enabled)
    {
        Assert.Equal(enabled, factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category).IsEnabled(level));
    }
}
