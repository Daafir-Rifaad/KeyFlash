namespace LoqKeyFlash.Backlight;

internal enum BacklightLevel
{
    Off = 0,
    Low = 1,
    High = 2
}

internal interface IKeyboardBacklight : IDisposable
{
    string Name { get; }
    BacklightLevel GetLevel();
    void SetLevel(BacklightLevel level);
}
