using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Buffers.Binary;

namespace GamepadServer;

static class PacketParser
{
    private const int LB = 0x0100, RB = 0x0200;
    private const int L_THUMB = 0x0040, R_THUMB = 0x0080;
    private const int START = 0x0010, BACK = 0x0020;
    private const int GUIDE = 0x0800;
    private const int A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
    private const int UP = 0x0001, DOWN = 0x0002, LEFT = 0x0004, RIGHT = 0x0008;

    public static void Apply(ReadOnlySpan<byte> packet, IXbox360Controller pad)
    {
        ushort buttons = BinaryPrimitives.ReadUInt16LittleEndian(packet[4..]);
        sbyte lx = (sbyte)packet[6], ly = (sbyte)packet[7];
        sbyte rx = (sbyte)packet[8], ry = (sbyte)packet[9];
        byte lt = packet[10], rt = packet[11];

        pad.SetButtonState(Xbox360Button.A, (buttons & A) != 0);
        pad.SetButtonState(Xbox360Button.B, (buttons & B) != 0);
        pad.SetButtonState(Xbox360Button.X, (buttons & X) != 0);
        pad.SetButtonState(Xbox360Button.Y, (buttons & Y) != 0);
        pad.SetButtonState(Xbox360Button.LeftShoulder, (buttons & LB) != 0);
        pad.SetButtonState(Xbox360Button.RightShoulder, (buttons & RB) != 0);
        pad.SetButtonState(Xbox360Button.LeftThumb, (buttons & L_THUMB) != 0);
        pad.SetButtonState(Xbox360Button.RightThumb, (buttons & R_THUMB) != 0);
        pad.SetButtonState(Xbox360Button.Start, (buttons & START) != 0);
        pad.SetButtonState(Xbox360Button.Back, (buttons & BACK) != 0);
        pad.SetButtonState(Xbox360Button.Guide, (buttons & GUIDE) != 0);
        pad.SetButtonState(Xbox360Button.Up, (buttons & UP) != 0);
        pad.SetButtonState(Xbox360Button.Down, (buttons & DOWN) != 0);
        pad.SetButtonState(Xbox360Button.Left, (buttons & LEFT) != 0);
        pad.SetButtonState(Xbox360Button.Right, (buttons & RIGHT) != 0);

        pad.SetAxisValue(Xbox360Axis.LeftThumbX, Scale(lx));
        pad.SetAxisValue(Xbox360Axis.LeftThumbY, Scale(ly));
        pad.SetAxisValue(Xbox360Axis.RightThumbX, Scale(rx));
        pad.SetAxisValue(Xbox360Axis.RightThumbY, Scale(ry));
        pad.SetSliderValue(Xbox360Slider.LeftTrigger, lt);
        pad.SetSliderValue(Xbox360Slider.RightTrigger, rt);

        pad.SubmitReport();
    }

    private static short Scale(sbyte v) => (short)(v * 256);
}
