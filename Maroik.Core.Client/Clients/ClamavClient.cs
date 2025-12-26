using Maroik.Core.Client.Contracts;
using System.Net;
using System.Net.Sockets;

namespace Maroik.Core.Client.Clients;

public class ClamavClient : IClamavClient
{
    public async Task<bool> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort)
    {
        try
        {
            fileStream.Position = 0;

            using var client = new TcpClient(clamavHost, clamavPort);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };

            await writer.WriteLineAsync("nINSTREAM");

            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                await stream.WriteAsync(buffer, 0, bytesRead);
            }

            byte[] zero = BitConverter.GetBytes(0);
            await stream.WriteAsync(zero, 0, zero.Length);

            using var reader = new StreamReader(stream);
            string response = await reader.ReadLineAsync() ?? string.Empty;

            return response.Contains("OK");
        }
        catch
        {
            return false;
        }
    }
}