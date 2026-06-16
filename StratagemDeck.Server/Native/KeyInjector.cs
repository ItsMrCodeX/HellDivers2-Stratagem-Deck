using System.Runtime.InteropServices;

namespace HD2Companion.Server.Native;

public static class KeyInjector
{
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static readonly int InputSize = IntPtr.Size == 8 ? Marshal.SizeOf<INPUT64>() : Marshal.SizeOf<INPUT32>();

    private static readonly Dictionary<string, ushort> VkMap = new()
    {
        ["up"] = 0x26,
        ["down"] = 0x28,
        ["left"] = 0x25,
        ["right"] = 0x27,
        ["ctrl"] = 0xA2,
    };

    // x64: sizeof = 40
    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT64
    {
        [FieldOffset(0)] public uint type;
        [FieldOffset(8)] public KEYBDINPUT ki;
        [FieldOffset(8)] private MOUSEINPUT _miPad;
    }

    // x86: sizeof = 28
    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT32
    {
        [FieldOffset(0)] public uint type;
        [FieldOffset(4)] public KEYBDINPUT ki;
        [FieldOffset(4)] private MOUSEINPUT _miPad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT64 pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT32 pInputs, int cbSize);

    public static async Task ExecuteSequence(List<string> keys, int delayMs = 40)
    {
        if (keys.Count == 0) return;

        SendSingle("ctrl", false);
        await Task.Delay(delayMs);

        foreach (var key in keys)
        {
            SendSingle(key, false);
            await Task.Delay(delayMs);
            SendSingle(key, true);
            await Task.Delay(delayMs);
        }

        SendSingle("ctrl", true);
        await Task.Delay(50);
    }

    private static void SendSingle(string key, bool keyUp)
    {
        if (!VkMap.TryGetValue(key, out var vk)) return;

        uint flags = keyUp ? KEYEVENTF_KEYUP : 0u;

        if (IntPtr.Size == 8)
        {
            var input = new INPUT64
            {
                type = 1,
                ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = flags, time = 0, dwExtraInfo = IntPtr.Zero }
            };
            SendInput(1, ref input, InputSize);
        }
        else
        {
            var input = new INPUT32
            {
                type = 1,
                ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = flags, time = 0, dwExtraInfo = IntPtr.Zero }
            };
            SendInput(1, ref input, InputSize);
        }
    }
}
