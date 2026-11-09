namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Reads the server-resource reports the host's resource-monitoring cron job writes. Each read returns the report's raw text, or an
/// empty string when the report does not exist (yet); making sense of the text is the caller's.
/// </summary>
public interface IServerResourceReportClient
{
    /// <summary>The host's CPU / memory / disk report.</summary>
    string ReadHostReport();

    /// <summary>The per-container <c>docker stats</c> report.</summary>
    string ReadDockerReport();
}
