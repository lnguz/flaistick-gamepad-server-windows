using System.Net;
using System.Net.Sockets;

namespace GamepadServer;

sealed class UdpServer(int port, ControllerHub hub)
{
    public async Task RunAsync(CancellationToken ct)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        Console.WriteLine($"Listening for gamepad packets on UDP port {port}...");

        var buffer = new byte[18];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
        var packetCount = 0L;

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

            var len = result.ReceivedBytes;

            if (len == 12)
            {
                int deviceId = BitConverter.ToInt32(buffer, 0);
                var pad = hub.Get(deviceId);
                PacketParser.Apply(buffer.AsSpan(0, 12), pad);

                packetCount++;
                if (packetCount % 200 == 1)
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] packet #{packetCount} from device {deviceId} (from {result.RemoteEndPoint})");
            }
            else if (len >= 6 && buffer[0] == ReorderPacket.Opcode && len == ReorderPacket.ExpectedLength(buffer[1]))
            {
                var order = ReorderPacket.ParseDeviceOrder(buffer, len);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] player order requested: [{string.Join(", ", order)}]");
                await hub.ReorderAsync(order);
            }
        }
    }
}
