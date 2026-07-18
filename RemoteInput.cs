using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GamepadServer;

static class RemoteInput
{
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;

    private const uint MouseEventMove = 0x0001;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventMiddleDown = 0x0020;
    private const uint MouseEventMiddleUp = 0x0040;
    private const uint MouseEventWheel = 0x0800;

    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    public static void MoveMouseRelative(int dx, int dy)
    {
        var input = new Input
        {
            Type = InputMouse,
            Data = new InputUnion { Mouse = new MouseInput { Dx = dx, Dy = dy, Flags = MouseEventMove } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    public static void MouseButton(int button, bool down)
    {
        var flags = button switch
        {
            0 => down ? MouseEventLeftDown : MouseEventLeftUp,
            1 => down ? MouseEventRightDown : MouseEventRightUp,
            _ => down ? MouseEventMiddleDown : MouseEventMiddleUp
        };
        var input = new Input
        {
            Type = InputMouse,
            Data = new InputUnion { Mouse = new MouseInput { Flags = flags } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    public static void MouseScroll(int notches)
    {
        var input = new Input
        {
            Type = InputMouse,
            Data = new InputUnion { Mouse = new MouseInput { MouseData = (uint)(notches * 120), Flags = MouseEventWheel } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    public static void SendText(string text)
    {
        var inputs = new List<Input>(text.Length * 2);
        foreach (var c in text)
        {
            inputs.Add(UnicodeCharInput(c, down: true));
            inputs.Add(UnicodeCharInput(c, down: false));
        }
        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
    }

    public static void KeyPress(ushort virtualKey)
    {
        KeyEvent(virtualKey, down: true);
        KeyEvent(virtualKey, down: false);
    }

    public static void KeyEvent(ushort virtualKey, bool down)
    {
        var input = new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = down ? 0 : KeyEventKeyUp } }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
    }

    /// Presses every key down in order, then releases them in reverse order (e.g. Alt+F4).
    public static void KeyCombo(ushort[] virtualKeys)
    {
        var inputs = new List<Input>(virtualKeys.Length * 2);
        foreach (var vk in virtualKeys)
            inputs.Add(new Input { Type = InputKeyboard, Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = vk } } });
        for (var i = virtualKeys.Length - 1; i >= 0; i--)
            inputs.Add(new Input { Type = InputKeyboard, Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = virtualKeys[i], Flags = KeyEventKeyUp } } });
        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
    }

    /// action: 0 = shutdown, 1 = sleep/suspend. Two fixed, hardcoded system commands — not arbitrary execution.
    public static void PowerCommand(byte action)
    {
        try
        {
            switch (action)
            {
                case 0:
                    Process.Start(new ProcessStartInfo("shutdown", "/s /t 0") { UseShellExecute = false, CreateNoWindow = true });
                    break;
                case 1:
                    Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0") { UseShellExecute = false, CreateNoWindow = true });
                    break;
            }
        }
        catch { /* best effort */ }
    }

    private static Input UnicodeCharInput(char c, bool down)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = 0,
                    ScanCode = c,
                    Flags = KeyEventUnicode | (down ? 0 : KeyEventKeyUp)
                }
            }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numInputs, Input[] inputs, int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
