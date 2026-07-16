using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GamepadServer;

sealed class DiscoveryServer(int discoveryPort, int gamePort)
{
    private static readonly byte[] RequestMagic = { 0x46, 0x4C, 0x53, 0x44 };  // "FLSD"
    private static readonly byte[] ResponseMagic = { 0x46, 0x4C, 0x53, 0x52 }; // "FLSR"

    public async Task RunAsync(CancellationToken ct)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
        Console.WriteLine($"Discovery listener active on UDP port {discoveryPort}...");

        var buffer = new byte[256];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!ct.IsCancellationRequested)
        {
            SocketReceiveFromResult result;
            try
            {
                result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (result.ReceivedBytes < 4 || !buffer.AsSpan(0, 4).SequenceEqual(RequestMagic)) continue;

            var hostname = Environment.MachineName;
            var nameBytes = Encoding.UTF8.GetBytes(hostname);
            var response = new byte[4 + 2 + 1 + nameBytes.Length];
            ResponseMagic.CopyTo(response, 0);
            BitConverter.GetBytes((ushort)gamePort).CopyTo(response, 4);
            response[6] = (byte)nameBytes.Length;
            nameBytes.CopyTo(response, 7);

            await socket.SendToAsync(response, SocketFlags.None, result.RemoteEndPoint, ct);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] discovery request from {result.RemoteEndPoint}, replied as '{hostname}'");
        }
    }
}
