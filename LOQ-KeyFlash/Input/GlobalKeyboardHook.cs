using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LoqKeyFlash.Input;

internal sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private readonly HookProc _callback;
    private readonly HashSet<int> _downKeys = [];
    private readonly object _keysLock = new();
    private IntPtr _hook;
    public bool IgnoreHeldKeyRepeats { get; set; }

    public event Action? KeyPressed;

    public GlobalKeyboardHook(bool ignoreHeldKeyRepeats = true)
    {
        IgnoreHeldKeyRepeats = ignoreHeldKeyRepeats;
        _callback = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = GetModuleHandle(module?.ModuleName);
        _hook = SetWindowsHookEx(WhKeyboardLl, _callback, moduleHandle, 0);
        if (_hook == IntPtr.Zero)
            throw new InvalidOperationException("Could not install the global keyboard hook.");
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var vk = Marshal.ReadInt32(data);
            var msg = message.ToInt32();

            if (msg is WmKeyDown or WmSysKeyDown)
            {
                var firstDown = false;
                lock (_keysLock)
                    firstDown = _downKeys.Add(vk);

                if (firstDown || !IgnoreHeldKeyRepeats)
                    KeyPressed?.Invoke();
            }
            else if (msg is WmKeyUp or WmSysKeyUp)
            {
                lock (_keysLock)
                    _downKeys.Remove(vk);
            }
        }

        return CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero)
            return;
        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
