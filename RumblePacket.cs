namespace GamepadServer;

/// Sent server -> phone (opposite direction of every other packet in this file) whenever a game
/// sets force-feedback on a virtual Xbox 360 controller, so the phone can vibrate the matching
/// physical gamepad.
static class RumblePacket
{
    public const byte Opcode = 0xB2;

    public static byte[] Build(int deviceId, byte largeMotor, byte smallMotor)
    {
        var buf = new byte[7];
        buf[0] = Opcode;
        BitConverter.GetBytes(deviceId).CopyTo(buf, 1);
        buf[5] = largeMotor;
        buf[6] = smallMotor;
        return buf;
    }
}
