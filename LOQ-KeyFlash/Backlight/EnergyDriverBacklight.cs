using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LoqKeyFlash.Backlight;

internal sealed class EnergyDriverBacklight : IKeyboardBacklight
{
    private const uint IoctlKeyboard = 0x83102144;
    private readonly SafeFileHandle _handle;

    public string Name => "Lenovo Energy Driver";

    private EnergyDriverBacklight(SafeFileHandle handle) => _handle = handle;

    public static EnergyDriverBacklight? TryCreate()
    {
        var handle = CreateFile(@"\\.\EnergyDrv", 0xC0000000, 0x00000003,
            IntPtr.Zero, 3, 0x80, IntPtr.Zero);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }

        try
        {
            var feature = new EnergyDriverBacklight(handle);
            var probe = feature.Send(0x1) >> 1;
            if (probe != 0x2)
            {
                feature.Dispose();
                return null;
            }

            _ = feature.GetLevel();
            return feature;
        }
        catch
        {
            handle.Dispose();
            return null;
        }
    }

    public BacklightLevel GetLevel() => Send(0x22) switch
    {
        0x1 => BacklightLevel.Off,
        0x3 => BacklightLevel.Low,
        0x5 => BacklightLevel.High,
        var value => throw new InvalidOperationException($"Unexpected keyboard-light state: 0x{value:X}.")
    };

    public void SetLevel(BacklightLevel level)
    {
        var code = level switch
        {
            BacklightLevel.Off => 0x00023u,
            BacklightLevel.Low => 0x10023u,
            BacklightLevel.High => 0x20023u,
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        _ = Send(code);
    }

    private uint Send(uint value)
    {
        if (!DeviceIoControl(_handle, IoctlKeyboard, ref value, sizeof(uint),
                out var output, sizeof(uint), out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Lenovo Energy Driver command failed.");

        return output;
    }

    public void Dispose() => _handle.Dispose();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess,
        uint shareMode, IntPtr securityAttributes, uint creationDisposition,
        uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint controlCode,
        ref uint input, int inputSize, out uint output, int outputSize,
        out uint bytesReturned, IntPtr overlapped);
}
