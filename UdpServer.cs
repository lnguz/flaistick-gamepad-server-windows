using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GamepadServer;

sealed class UdpServer(int port, ControllerHub hub)
{
    private Socket? _socket;
    private EndPoint? _lastClient;
    private readonly ConcurrentDictionary<int, (byte Large, byte Small)> _lastLoggedRumble = new();

    public async Task RunAsync(CancellationToken ct)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        Console.WriteLine($"Listening for gamepad packets on UDP port {port}...");

        _socket = socket;
        hub.RumbleReceived += OnRumbleReceived;

        var buffer = new byte[260];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
        var packetCount = 0L;

        try
        {
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
                _lastClient = result.RemoteEndPoint;

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
                else
                {
                    RemoteInputPacket.TryHandle(buffer.AsSpan(0, len));
                }
            }
        }
        finally
        {
            hub.RumbleReceived -= OnRumbleReceived;
            _socket = null;
        }
    }

    private void OnRumbleReceived(int deviceId, byte largeMotor, byte smallMotor)
    {
        if (_lastLoggedRumble.GetOrAdd(deviceId, (0, 0)) != (largeMotor, smallMotor))
        {
            _lastLoggedRumble[deviceId] = (largeMotor, smallMotor);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] rumble for device {deviceId}: large={largeMotor} small={smallMotor} (client={_lastClient?.ToString() ?? "none"})");
        }

        var socket = _socket;
        var client = _lastClient;
        if (socket is null || client is null) return;
        try
        {
            socket.SendTo(RumblePacket.Build(deviceId, largeMotor, smallMotor), client);
        }
        catch (SocketException)
        {
            // Phone disconnected/unreachable — the next gamepad packet will refresh _lastClient.
        }
    }
}
