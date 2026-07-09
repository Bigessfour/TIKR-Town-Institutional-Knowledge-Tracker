using System.Net;
using System.Net.Sockets;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// HTTP probe for Ollama that prefers IPv4 when resolving hostnames (Docker Desktop often advertises IPv6 for host.docker.internal while Ollama listens on IPv4).
/// </summary>
internal static class OllamaHttpProbe
{
    public static HttpClient CreateClient(TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectPreferIpv4Async,
        };
        return new HttpClient(handler) { Timeout = timeout };
    }

    private static async ValueTask<Stream> ConnectPreferIpv4Async(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        IPAddress? address = null;
        if (IPAddress.TryParse(host, out var literal))
        {
            address = literal;
        }
        else
        {
            var entry = await Dns.GetHostEntryAsync(host, cancellationToken);
            address = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                ?? entry.AddressList.FirstOrDefault();
        }

        if (address is null)
            throw new SocketException((int)SocketError.HostNotFound);

        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
