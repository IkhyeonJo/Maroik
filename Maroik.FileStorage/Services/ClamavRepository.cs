using Maroik.FileStorage.Contracts;
using System.Net;
using System.Net.Sockets;

namespace Maroik.FileStorage.Services;

public class ClamavRepository : IClamavRepository
{
    private const string ClamavStorageDomain = "Maroik.Clamav";

    public async Task<bool> ScanWithClamavAsync(Stream fileStream)
    {
        try
        {
            fileStream.Position = 0;

            using var client = new TcpClient(ClamavStorageDomain, 3310);
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.AutoFlush = true;

            await writer.WriteLineAsync("nINSTREAM");

            var buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
            {
                var lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                await stream.WriteAsync(lengthPrefix);
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            var zero = BitConverter.GetBytes(0);
            await stream.WriteAsync(zero);

            using var reader = new StreamReader(stream);
            var response = await reader.ReadLineAsync() ?? string.Empty;

            return response.Contains("OK");
        }
        catch
        {
            return false;
        }
    }
}