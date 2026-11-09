using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Implementation of <see cref="IServerResourceReportClient"/> over the two report files the host's cron job writes and the compose
/// files mount into the container.
/// </summary>
/// <param name="hostReportPath">Path of the host report (<see cref="HostReportPath"/> in a deployment).</param>
/// <param name="dockerReportPath">Path of the container report (<see cref="DockerReportPath"/> in a deployment).</param>
public class ServerResourceReportClient(string hostReportPath, string dockerReportPath) : IServerResourceReportClient
{
    /// <summary>Host CPU / memory / disk report written by the host's resource-monitoring cron job (mounted into the container).</summary>
    public const string HostReportPath = "/app/Maroik.Log/HostResourceInfo.txt";

    /// <summary>Per-container <c>docker stats</c> report written by the same cron job.</summary>
    public const string DockerReportPath = "/app/Maroik.Log/DockerResourceInfo.txt";

    /// <inheritdoc />
    public string ReadHostReport() => Read(hostReportPath);

    /// <inheritdoc />
    public string ReadDockerReport() => Read(dockerReportPath);

    /// <summary>The text of the file at <paramref name="path"/>, or an empty string when there is no such file.</summary>
    private static string Read(string path) => File.Exists(path) ? File.ReadAllText(path) : "";
}
