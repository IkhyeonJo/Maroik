using Maroik.Core.Client.Clients;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>The client hands over each report file's text as it is, and an empty string for a report that does not exist.</summary>
public class ServerResourceReportClientTests
{
    /// <summary>Each read returns the text of its own file.</summary>
    [Fact]
    public void Reads_ReturnEachReportsOwnText()
    {
        string hostFile = Path.GetTempFileName();
        string dockerFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(hostFile, "host report");
            File.WriteAllText(dockerFile, "docker report");
            var sut = new ServerResourceReportClient(hostFile, dockerFile);

            Assert.Equal("host report", sut.ReadHostReport());
            Assert.Equal("docker report", sut.ReadDockerReport());
        }
        finally
        {
            File.Delete(hostFile);
            File.Delete(dockerFile);
        }
    }

    /// <summary>A report the cron job has not written yet reads as empty text, not as a failure.</summary>
    [Fact]
    public void Reads_ReturnEmptyText_WhenTheReportDoesNotExist()
    {
        var sut = new ServerResourceReportClient("nonexistent_host.txt", "nonexistent_docker.txt");

        Assert.Equal("", sut.ReadHostReport());
        Assert.Equal("", sut.ReadDockerReport());
    }
}
