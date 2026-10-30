using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// How the host <see cref="WorkerHost"/> builds logs: to the console only, one line per entry with the scopes a message is handled
/// in (its delivery tag and correlation id), at Information and above (Microsoft's chatter at Warning). Logged values come from
/// queued messages (a recipient address …), so a value must not be able to end the line and start a forged entry of its own.
/// </summary>
public sealed class WorkerHostLoggingTests : IDisposable
{
    /// <summary>The host under test (built, never started; the broker address is never dialled).</summary>
    private readonly IHost _host = WorkerHost.Build(
        ["--ConnectionStrings:RabbitMQ=amqp://guest:guest@127.0.0.1:5672/", "--ConnectionStrings:DefaultConnection=Host=127.0.0.1;Port=1;Database=unused;Username=u;Password=p"]);

    /// <inheritdoc />
    public void Dispose() => _host.Dispose();

    /// <summary>Renders one entry, inside <paramref name="scopes"/>, through the host's console formatter.</summary>
    private string Render(LogLevel level, string message, IExternalScopeProvider? scopes = null, Exception? exception = null)
    {
        string? name = _host.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName;
        ConsoleFormatter formatter = _host.Services.GetServices<ConsoleFormatter>().Single(f => f.Name == name);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        formatter.Write(new LogEntry<string>(level, "Test", new EventId(0), message, exception, (state, _) => state), scopes, writer);
        return writer.ToString();
    }

    /// <summary>The host's default providers (debug, event source …) are gone; it logs to the console alone.</summary>
    [Fact]
    public void TheHost_LogsToTheConsoleOnly()
    {
        Assert.IsType<ConsoleLoggerProvider>(Assert.Single(_host.Services.GetServices<ILoggerProvider>()));
    }

    /// <summary>The simple formatter writes one uncoloured line per entry, stamped with the time of day.</summary>
    [Fact]
    public void TheConsole_UsesTheSimpleFormatter_OnOneUncolouredLine()
    {
        Assert.Equal(ConsoleFormatterNames.Simple, _host.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue.FormatterName);
        SimpleConsoleFormatterOptions options = _host.Services.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;
        Assert.True(options.SingleLine);
        Assert.Equal(LoggerColorBehavior.Disabled, options.ColorBehavior);
        Assert.Matches(@"^\d{2}:\d{2}:\d{2} warn: Test\[0\] Email send failed\r?\n$", Render(LogLevel.Warning, "Email send failed"));
    }

    /// <summary>The scopes a message is handled in (its delivery tag, then its correlation id) are written before the message.</summary>
    [Fact]
    public void TheConsole_WritesTheScopes()
    {
        var scopes = new LoggerExternalScopeProvider();

        using (scopes.Push("DeliveryTag: 7"))
        using (scopes.Push("CorrelationId: c-1"))
        {
            Assert.Matches(@"^\d{2}:\d{2}:\d{2} warn: Test\[0\] => DeliveryTag: 7 => CorrelationId: c-1 Email send failed\r?\n$",
                Render(LogLevel.Warning, "Email send failed", scopes));
        }
    }

    /// <summary>A line break inside a logged value cannot end the entry's line, so it cannot start a forged entry.</summary>
    [Fact]
    public void TheConsole_KeepsAValueWithALineBreakOnTheEntrysLine()
    {
        string rendered = Render(LogLevel.Warning, "Email send failed for a@b.com\n00:00:00 info: Maroik[0] Email sent to admin@maroik.com");

        Assert.EndsWith(Environment.NewLine, rendered, StringComparison.Ordinal);
        string line = rendered[..^Environment.NewLine.Length];
        Assert.DoesNotContain('\n', line);
        Assert.EndsWith("failed for a@b.com 00:00:00 info: Maroik[0] Email sent to admin@maroik.com", line, StringComparison.Ordinal);
    }

    /// <summary>An exception (its message and stack trace) is written on the entry's line as well.</summary>
    [Fact]
    public void TheConsole_WritesTheExceptionOnTheEntrysLine()
    {
        string line = Render(LogLevel.Error, "Email send failed", exception: new InvalidOperationException("first\nsecond"))[..^Environment.NewLine.Length];

        Assert.DoesNotContain('\n', line);
        Assert.Contains("Email send failed System.InvalidOperationException: first second", line, StringComparison.Ordinal);
    }

    /// <summary>Information and above is logged, except Microsoft's chatter below Warning; the host lifetime stays at Information.</summary>
    [Theory]
    [InlineData("Maroik.Worker.Workers.EmailConsumerWorker", LogLevel.Information, true)]
    [InlineData("Maroik.Worker.Workers.EmailConsumerWorker", LogLevel.Debug, false)]
    [InlineData("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information, false)]
    [InlineData("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning, true)]
    [InlineData("Microsoft.Hosting.Lifetime", LogLevel.Information, true)]
    [InlineData("System.Net.Http.HttpClient.Default.LogicalHandler", LogLevel.Information, true)]
    public void TheHost_LogsInformationAndAbove_AndMicrosoftFromWarning(string category, LogLevel level, bool enabled)
    {
        Assert.Equal(enabled, _host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category).IsEnabled(level));
    }
}
