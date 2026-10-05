using Microsoft.Win32;

namespace LoqKeyFlash;

internal static class StartupManager
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LOQ KeyFlash";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
            if (key?.GetValue(ValueName) is not string value)
                return false;

            var expectedPrefix = $"\"{Application.ExecutablePath}\"";
            return value.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        if (enabled)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\" --startup");
        else
            key.DeleteValue(ValueName, false);
    }
}
