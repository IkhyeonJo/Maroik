using System.Diagnostics;

namespace Maroik.Website.Tests;

/// <summary>
/// The executable itself (<c>Program.cs</c>), run as a real process: when it cannot start it logs one critical console entry with
/// the exception and exits with a failure code.
/// </summary>
public class ProgramProcessTests
{
    /// <summary>Without any settings (no session lifetime) it cannot start: it logs the reason as a critical entry and exits with a failure code.</summary>
    [Fact]
    public async Task TheExecutable_LogsACriticalEntryAndFails_WhenItCannotStart()
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "Maroik.Website.dll");
        var info = new ProcessStartInfo("dotnet", $"\"{dll}\"")
        {
            WorkingDirectory = AppContext.BaseDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            Environment = { ["DOTNET_ENVIRONMENT"] = "Production", ["ASPNETCORE_ENVIRONMENT"] = "Production" }
        };
        using Process process = Process.Start(info)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        string output = await stdout;
        _ = await stderr;

        Assert.NotEqual(0, process.ExitCode);
        Assert.Matches(@"(?m)^\d{2}:\d{2}:\d{2} crit: Maroik\.Website\[0\] Maroik\.Website terminated unexpectedly System\.InvalidOperationException: ServerSetting:SessionExpireMinutes must be a positive number\.", output);
    }
}
