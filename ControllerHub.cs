using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System.Collections.Concurrent;

namespace GamepadServer;

sealed class ControllerHub : IDisposable
{
    private readonly ViGEmClient _client = new();
    private readonly ConcurrentDictionary<int, (IXbox360Controller Pad, long LastSeen)> _pads = new();
    private readonly System.Threading.Timer _sweeper;

    public ControllerHub()
    {
        _sweeper = new System.Threading.Timer(_ => Sweep(), null, 2000, 2000);
    }

    public IXbox360Controller Get(int deviceId)
    {
        var now = Environment.TickCount64;
        var entry = _pads.AddOrUpdate(deviceId,
            _ =>
            {
                var pad = _client.CreateXbox360Controller();
                pad.Connect();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] virtual Xbox 360 controller connected for device {deviceId}");
                return (pad, now);
            },
            (_, existing) => (existing.Pad, now));
        return entry.Pad;
    }

    public async Task ReorderAsync(int[] orderedDeviceIds)
    {
        var pads = new List<IXbox360Controller>();
        foreach (var id in orderedDeviceIds)
        {
            if (_pads.TryGetValue(id, out var entry))
            {
                entry.Pad.Disconnect();
                pads.Add(entry.Pad);
            }
        }
        if (pads.Count == 0) return;

        await Task.Delay(300);
        foreach (var pad in pads)
        {
            pad.Connect();
            await Task.Delay(300);
        }

        var now = Environment.TickCount64;
        foreach (var id in orderedDeviceIds)
        {
            if (_pads.TryGetValue(id, out var entry))
                _pads[id] = (entry.Pad, now);
        }
    }

    private void Sweep()
    {
        var now = Environment.TickCount64;
        foreach (var kvp in _pads)
        {
            if (now - kvp.Value.LastSeen > 3000 && _pads.TryRemove(kvp.Key, out var e))
            {
                e.Pad.Disconnect();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] controller for device {kvp.Key} timed out, disconnected");
            }
        }
    }

    public void Dispose()
    {
        _sweeper.Dispose();
        foreach (var kvp in _pads) kvp.Value.Pad.Disconnect();
        _client.Dispose();
    }
}
