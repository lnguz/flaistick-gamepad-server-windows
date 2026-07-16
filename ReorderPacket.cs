namespace GamepadServer;

static class ReorderPacket
{
    public const byte Opcode = 0xAA;

    public static int ExpectedLength(byte deviceCount) => 2 + 4 * deviceCount;

    public static int[] ParseDeviceOrder(ReadOnlySpan<byte> packet, int length)
    {
        int count = packet[1];
        var order = new int[count];
        for (int i = 0; i < count; i++)
            order[i] = BitConverter.ToInt32(packet.Slice(2 + 4 * i, 4));
        return order;
    }
}
