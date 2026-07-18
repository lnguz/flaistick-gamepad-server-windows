using System.Text;

namespace GamepadServer;

static class RemoteInputPacket
{
    public const byte OpcodeMouseMove = 0xAB;
    public const byte OpcodeMouseButton = 0xAC;
    public const byte OpcodeMouseScroll = 0xAD;
    public const byte OpcodeKeyEvent = 0xAE;
    public const byte OpcodeKeyCombo = 0xAF;
    public const byte OpcodeText = 0xB0;
    public const byte OpcodePower = 0xB1;

    public static bool TryHandle(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 1) return false;

        switch (packet[0])
        {
            case OpcodeMouseMove when packet.Length == 5:
                RemoteInput.MoveMouseRelative(
                    BitConverter.ToInt16(packet.Slice(1, 2)),
                    BitConverter.ToInt16(packet.Slice(3, 2)));
                return true;

            case OpcodeMouseButton when packet.Length == 3:
                RemoteInput.MouseButton(packet[1], packet[2] != 0);
                return true;

            case OpcodeMouseScroll when packet.Length == 3:
                RemoteInput.MouseScroll(BitConverter.ToInt16(packet.Slice(1, 2)));
                return true;

            case OpcodeKeyEvent when packet.Length == 4:
                RemoteInput.KeyEvent(BitConverter.ToUInt16(packet.Slice(2, 2)), packet[1] != 0);
                return true;

            case OpcodeKeyCombo when packet.Length >= 2 && packet.Length == 2 + 2 * packet[1]:
            {
                var count = packet[1];
                var keys = new ushort[count];
                for (var i = 0; i < count; i++)
                    keys[i] = BitConverter.ToUInt16(packet.Slice(2 + i * 2, 2));
                RemoteInput.KeyCombo(keys);
                return true;
            }

            case OpcodeText when packet.Length >= 2 && packet.Length == 2 + packet[1]:
            {
                var text = Encoding.UTF8.GetString(packet.Slice(2, packet[1]));
                RemoteInput.SendText(text);
                return true;
            }

            case OpcodePower when packet.Length == 2:
                RemoteInput.PowerCommand(packet[1]);
                return true;

            default:
                return false;
        }
    }
}
