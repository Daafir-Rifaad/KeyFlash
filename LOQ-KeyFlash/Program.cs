namespace LoqKeyFlash;

internal static class Program
{
    private const string MutexName = "Local\\LOQ-KeyFlash-Single-Instance";
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LOQ KeyFlash", "launch-error.log");

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, eventArgs) => ReportFatalError(eventArgs.Exception);

            using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
            if (!isFirstInstance)
            {
                MessageBox.Show("LOQ KeyFlash is already running in the system tray.",
                    "LOQ KeyFlash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var startHidden = args.Any(arg => string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));
            Application.Run(new MainForm(startHidden));
        }
        catch (Exception exception)
        {
            ReportFatalError(exception);
        }
    }

    private static void ReportFatalError(Exception exception)
    {
        try
        {
            var folder = Path.GetDirectoryName(CrashLogPath)!;
            Directory.CreateDirectory(folder);
            File.WriteAllText(CrashLogPath,
                $"LOQ KeyFlash launch failure{Environment.NewLine}" +
                $"Time: {DateTimeOffset.Now:O}{Environment.NewLine}" +
                $"Version: {Application.ProductVersion}{Environment.NewLine}" +
                $"Windows: {Environment.OSVersion}{Environment.NewLine}{Environment.NewLine}" +
                exception);
        }
        catch
        {
            // The message box below still reports the failure when logging is unavailable.
        }

        try
        {
            MessageBox.Show(
                $"LOQ KeyFlash could not start. A diagnostic report was written to:{Environment.NewLine}{CrashLogPath}{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "LOQ KeyFlash — Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Avoid a second failure if Windows cannot create a message box.
        }
    }
}
