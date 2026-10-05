namespace LoqKeyFlash.Backlight;

internal static class BacklightFactory
{
    public static IKeyboardBacklight? Create(bool useEnergyDriver, bool allowWmiFallback, out string diagnostic)
    {
        if (useEnergyDriver)
        {
            var driver = EnergyDriverBacklight.TryCreate();
            if (driver is not null)
            {
                diagnostic = "Connected through Lenovo Energy Driver.";
                return driver;
            }
        }

        if (allowWmiFallback)
        {
            var wmi = LenovoLightingBacklight.TryCreate();
            if (wmi is not null)
            {
                diagnostic = "Connected through Lenovo Lighting WMI.";
                return wmi;
            }
        }

        diagnostic = useEnergyDriver || allowWmiFallback
            ? "No enabled Lenovo white-keyboard interface was found. Update Lenovo Vantage/System Interface Foundation or enable another interface."
            : "Enable at least one hardware interface below.";
        return null;
    }
}
