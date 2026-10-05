using System.Management;

namespace LoqKeyFlash.Backlight;

internal sealed class LenovoLightingBacklight : IKeyboardBacklight
{
    private const string ScopePath = @"root\WMI";
    public string Name => "Lenovo Lighting WMI";

    public static LenovoLightingBacklight? TryCreate()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(ScopePath,
                "SELECT * FROM LENOVO_LIGHTING_DATA WHERE Lighting_ID = 0 AND Control_Interface = 0 AND Lighting_Type = 1");
            using var results = searcher.Get();
            if (results.Count == 0)
                return null;

            var backend = new LenovoLightingBacklight();
            _ = backend.GetLevel();
            return backend;
        }
        catch
        {
            return null;
        }
    }

    public BacklightLevel GetLevel()
    {
        using var method = GetMethodObject();
        using var args = method.GetMethodParameters("Get_Lighting_Current_Status");
        args["Lighting_ID"] = 0;
        using var result = method.InvokeMethod("Get_Lighting_Current_Status", args, null)
            ?? throw new InvalidOperationException("Lenovo Lighting returned no keyboard state.");

        var level = Convert.ToInt32(result["Current_Brightness_Level"]);
        return level switch
        {
            1 => BacklightLevel.Off,
            2 => BacklightLevel.Low,
            3 => BacklightLevel.High,
            _ => throw new InvalidOperationException($"Unexpected Lenovo Lighting level: {level}.")
        };
    }

    public void SetLevel(BacklightLevel level)
    {
        using var method = GetMethodObject();
        using var args = method.GetMethodParameters("Set_Lighting_Current_Status");
        args["Lighting_ID"] = 0;
        args["Current_State_Type"] = 0;
        args["Current_Brightness_Level"] = (int)level + 1;
        using var result = method.InvokeMethod("Set_Lighting_Current_Status", args, null);
    }

    private static ManagementObject GetMethodObject()
    {
        using var searcher = new ManagementObjectSearcher(ScopePath, "SELECT * FROM LENOVO_LIGHTING_METHOD");
        using var results = searcher.Get();
        return results.Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("LENOVO_LIGHTING_METHOD is unavailable.");
    }

    public void Dispose() { }
}
