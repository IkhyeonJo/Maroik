namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Parsed server and Docker resource metrics read from the log files
/// written by the host monitoring script.
/// Consumed by the admin dashboard view.
/// </summary>
public class ServerResourceSummary
{
    /// <summary>CPU usage as a numeric percentage (e.g. 12.5).</summary>
    public double HostCpuNumeric { get; init; }

    /// <summary>Raw CPU information string extracted from the log file.</summary>
    public string HostCpuInfo { get; init; } = string.Empty;

    /// <summary>Formatted memory usage/limit string (e.g. "16 GB / 8 GB").</summary>
    public string MemUsageLimit { get; init; } = string.Empty;

    /// <summary>Raw memory information string extracted from the log file.</summary>
    public string HostMemoryInfo { get; init; } = string.Empty;

    /// <summary>Formatted disk usage/limit string (e.g. "500 GB / 120 GB").</summary>
    public string DiskUsageLimit { get; init; } = string.Empty;

    /// <summary>Raw disk information string extracted from the log file.</summary>
    public string HostDiskInfo { get; init; } = string.Empty;

    /// <summary>
    /// Docker container resource rows. The first entry is always the header
    /// <c>"Name||CPU %||MEM Usage / LIMIT"</c>; subsequent entries are pipe-delimited data rows.
    /// </summary>
    public List<string> DockerContainerResult { get; init; } = ["Name||CPU %||MEM Usage / LIMIT"];
}
