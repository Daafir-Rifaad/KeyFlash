using System.Text.Json;

namespace LoqKeyFlash;

internal sealed class AppSettings
{
    public bool FlashingEnabled { get; set; }
    public bool IgnoreHeldKeyRepeats { get; set; } = true;
    public bool StartHidden { get; set; } = true;
    public bool UseEnergyDriver { get; set; } = true;
    public bool AllowWmiFallback { get; set; } = true;
    public int PulseMilliseconds { get; set; } = 32;
    public int GapMilliseconds { get; set; } = 12;

    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LOQ KeyFlash");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
            if (settings is null)
                return new AppSettings();

            settings.PulseMilliseconds = Math.Clamp(settings.PulseMilliseconds, 15, 120);
            settings.GapMilliseconds = Math.Clamp(settings.GapMilliseconds, 5, 60);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
