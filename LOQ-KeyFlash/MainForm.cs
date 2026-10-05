using System.Runtime.InteropServices;
using Microsoft.Win32;
using LoqKeyFlash.Backlight;
using LoqKeyFlash.Input;
using LoqKeyFlash.UI;

namespace LoqKeyFlash;

internal sealed class MainForm : Form
{
    private const int StartupSettleDelayMilliseconds = 8000;
    private const int RecoveryDelayMilliseconds = 1600;
    private const int RecoveryRetryMilliseconds = 3500;
    private const int HealthIntervalMilliseconds = 5000;

    private readonly AppSettings _settings;
    private readonly bool _launchedForStartup;
    private readonly NotifyIcon _trayIcon;
    private readonly AuroraCheckBox _flashCheckbox;
    private readonly AuroraCheckBox _ignoreHeldCheckbox;
    private readonly AuroraCheckBox _startupCheckbox;
    private readonly AuroraCheckBox _silentStartCheckbox;
    private readonly AuroraCheckBox _energyDriverCheckbox;
    private readonly AuroraCheckBox _wmiCheckbox;
    private readonly AuroraSlider _durationSlider;
    private readonly AuroraValueBox _durationValue;
    private readonly Label _statusLabel;
    private readonly AuroraButton _testButton;
    private readonly AuroraButton _reconnectButton;
    private readonly System.Windows.Forms.Timer _startupTimer;
    private readonly System.Windows.Forms.Timer _healthTimer;
    private readonly Bitmap _appMarkImage;
    private readonly Bitmap _githubImage;
    private readonly ToolTip _toolTip;

    private IKeyboardBacklight? _backlight;
    private FlashEngine? _engine;
    private GlobalKeyboardHook? _hook;
    private CancellationTokenSource? _recoveryCancellation;
    private bool _sessionUnavailable;
    private bool _initializing;
    private bool _allowClose;
    private bool _updatingControls;
    private bool _updatingDuration;

    public MainForm(bool startHidden = false)
    {
        _settings = AppSettings.Load();
        _launchedForStartup = startHidden;

        Text = "LOQ KeyFlash";
        ClientSize = new Size(600, 770);
        MinimumSize = new Size(600, 770);
        MaximumSize = new Size(600, 770);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AuroraPalette.Canvas;
        ForeColor = AuroraPalette.Text;
        Font = new Font("Space Grotesk", 10f, FontStyle.Regular);
        DoubleBuffered = true;

        _appMarkImage = BrandAssets.LoadBitmap("loq-keyflash-mark.png");
        _githubImage = BrandAssets.LoadBitmap("github-mark.png");
        _toolTip = new ToolTip
        {
            InitialDelay = 350,
            ReshowDelay = 100,
            AutoPopDelay = 5000,
            BackColor = AuroraPalette.SurfaceRaised,
            ForeColor = AuroraPalette.Text
        };
        try
        {
            var executableIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (executableIcon is not null)
                Icon = executableIcon;
        }
        catch
        {
            // The embedded application icon remains available through Windows.
        }

        var chrome = CreateChrome();
        Controls.Add(chrome);

        var identityPanel = new AuroraPanel
        {
            Bounds = new Rectangle(26, 58, 548, 148)
        };
        var brandBadge = new AuroraBrandBadge
        {
            Bounds = new Rectangle(20, 14, 120, 120),
            Image = _appMarkImage,
            AccessibleName = "KeyFlash H3 logo"
        };
        var identityDivider = new Panel
        {
            Bounds = new Rectangle(165, 24, 1, 100),
            BackColor = AuroraPalette.BorderSoft
        };
        var creditLead = new Label
        {
            Text = "Build & Design by",
            UseMnemonic = false,
            ForeColor = AuroraPalette.Muted,
            BackColor = Color.Transparent,
            Font = new Font("Space Grotesk", 9.2f),
            Location = new Point(192, 27),
            AutoSize = true
        };
        var authorName = new Label
        {
            Text = "Rifaad Daafir",
            ForeColor = AuroraPalette.Text,
            BackColor = Color.Transparent,
            Font = new Font("Space Grotesk SemiBold", 16f, FontStyle.Bold),
            Location = new Point(190, 49),
            AutoSize = true
        };
        var githubButton = CreateButton("github.com/Daafir-Rifaad", new Rectangle(190, 89, 302, 36));
        githubButton.Image = _githubImage;
        githubButton.AccessibleName = "Open Rifaad Daafir on GitHub";
        githubButton.Click += (_, _) => OpenGitHubProfile();
        _toolTip.SetToolTip(githubButton, "Open github.com/Daafir-Rifaad");
        identityPanel.Controls.AddRange([
            brandBadge, identityDivider, creditLead, authorName, githubButton
        ]);
        Controls.Add(identityPanel);

        var lightingPanel = CreateSection("Lighting", new Rectangle(26, 222, 548, 210));
        _flashCheckbox = CreateCheckBox("Flash", new Rectangle(24, 48, 210, 30), _settings.FlashingEnabled);
        _ignoreHeldCheckbox = CreateCheckBox("Ignore on held", new Rectangle(272, 48, 230, 30), _settings.IgnoreHeldKeyRepeats);
        var durationLabel = CreateFieldLabel("Flash duration", new Point(24, 101));
        _durationSlider = new AuroraSlider
        {
            Location = new Point(20, 135),
            Width = 378,
            Minimum = 15,
            Maximum = 120,
            Value = _settings.PulseMilliseconds
        };
        _durationValue = new AuroraValueBox
        {
            Location = new Point(424, 133),
            Minimum = 15,
            Maximum = 120,
            Value = _settings.PulseMilliseconds
        };
        lightingPanel.Controls.AddRange([
            _flashCheckbox, _ignoreHeldCheckbox, durationLabel,
            _durationSlider, _durationValue
        ]);

        var systemPanel = CreateSection("System", new Rectangle(26, 448, 548, 116));
        _startupCheckbox = CreateCheckBox("Run at startup", new Rectangle(24, 50, 220, 30), StartupManager.IsEnabled);
        _silentStartCheckbox = CreateCheckBox("Silent start", new Rectangle(272, 50, 220, 30), _settings.StartHidden);
        systemPanel.Controls.AddRange([_startupCheckbox, _silentStartCheckbox]);

        var interfacePanel = CreateSection("Interface", new Rectangle(26, 580, 548, 116));
        _energyDriverCheckbox = CreateCheckBox("Lenovo EnergyDrv", new Rectangle(24, 50, 220, 30), _settings.UseEnergyDriver);
        _wmiCheckbox = CreateCheckBox("Lenovo Lighting WMI", new Rectangle(272, 50, 240, 30), _settings.AllowWmiFallback);
        interfacePanel.Controls.AddRange([_energyDriverCheckbox, _wmiCheckbox]);

        Controls.AddRange([lightingPanel, systemPanel, interfacePanel]);

        _statusLabel = new Label
        {
            Text = _launchedForStartup ? "Waiting for Lenovo services…" : "Initializing…",
            ForeColor = AuroraPalette.Muted,
            BackColor = Color.Transparent,
            Font = new Font("Space Grotesk", 9.2f),
            Location = new Point(32, 718),
            Size = new Size(322, 34),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _reconnectButton = CreateButton("Reconnect", new Rectangle(366, 718, 106, 34));
        _testButton = CreateButton("Test", new Rectangle(480, 718, 88, 34));
        _testButton.Enabled = false;
        Controls.AddRange([_statusLabel, _reconnectButton, _testButton]);

        var windowFrame = new AuroraWindowFrame
        {
            Bounds = ClientRectangle,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(windowFrame);
        windowFrame.BringToFront();

        _flashCheckbox.CheckedChanged += FlashCheckboxChanged;
        _ignoreHeldCheckbox.CheckedChanged += BehaviorOptionChanged;
        _startupCheckbox.CheckedChanged += StartupCheckboxChanged;
        _silentStartCheckbox.CheckedChanged += BehaviorOptionChanged;
        _energyDriverCheckbox.CheckedChanged += HardwareOptionChanged;
        _wmiCheckbox.CheckedChanged += HardwareOptionChanged;
        _durationSlider.ValueChanged += DurationSliderChanged;
        _durationValue.ValueChanged += DurationValueChanged;
        _testButton.Click += TestButtonClicked;
        _reconnectButton.Click += (_, _) => ScheduleRecovery("Reconnecting…", 0, true);

        var trayMenu = new ContextMenuStrip
        {
            BackColor = AuroraPalette.SurfaceRaised,
            ForeColor = AuroraPalette.Text,
            ShowImageMargin = false
        };
        trayMenu.Items.Add("Open LOQ KeyFlash", null, (_, _) => ShowFromTray());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = "LOQ KeyFlash",
            Visible = true,
            ContextMenuStrip = trayMenu
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        SystemEvents.SessionSwitch += SystemEventsOnSessionSwitch;
        SystemEvents.PowerModeChanged += SystemEventsOnPowerModeChanged;

        _healthTimer = new System.Windows.Forms.Timer { Interval = HealthIntervalMilliseconds };
        _healthTimer.Tick += (_, _) => HealthCheck();

        _startupTimer = new System.Windows.Forms.Timer
        {
            Interval = _launchedForStartup ? StartupSettleDelayMilliseconds : 120
        };
        _startupTimer.Tick += (_, _) =>
        {
            _startupTimer.Stop();
            if (!InitializeHardware() && _settings.FlashingEnabled)
                ScheduleRecovery("Waiting for keyboard interface…", RecoveryRetryMilliseconds);
            _healthTimer.Start();
        };

        _startupTimer.Start();
    }

    private Panel CreateChrome()
    {
        var chrome = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = AuroraPalette.Chrome
        };
        chrome.MouseDown += BeginWindowDrag;

        var title = new Label
        {
            Text = "LOQ KeyFlash",
            ForeColor = AuroraPalette.Text,
            BackColor = Color.Transparent,
            Font = new Font("Space Grotesk Medium", 9.2f, FontStyle.Bold),
            Location = new Point(16, 0),
            Size = new Size(220, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };
        title.MouseDown += BeginWindowDrag;

        var minimize = CreateButton("—", new Rectangle(510, 6, 36, 30));
        minimize.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        var close = CreateButton("×", new Rectangle(552, 6, 36, 30));
        close.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        close.IsDanger = true;
        close.Click += (_, _) => Close();

        chrome.Controls.AddRange([title, minimize, close]);
        return chrome;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_launchedForStartup && _settings.StartHidden)
            BeginInvoke(new Action(Hide));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        MechaScribePainter.Draw(e.Graphics, ClientRectangle);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = AuroraGeometry.CutPath(ClientRectangle,
            AuroraGeometry.CutDepth(ClientRectangle, 24, 4));
        Region = new Region(path);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(1400, "LOQ KeyFlash", "Running in the system tray.", ToolTipIcon.Info);
            return;
        }

        base.OnFormClosing(e);
    }

    private bool InitializeHardware()
    {
        if (_initializing || _sessionUnavailable || IsDisposed)
            return false;

        _initializing = true;
        try
        {
            ShutdownHardware();
            _backlight = BacklightFactory.Create(_settings.UseEnergyDriver, _settings.AllowWmiFallback, out var diagnostic);
            if (_backlight is null)
            {
                _testButton.Enabled = false;
                SetStatus(diagnostic, true);
                return false;
            }

            _engine = new FlashEngine(_backlight, _settings.PulseMilliseconds, _settings.GapMilliseconds);
            _engine.Faulted += message => PostToUi(() => HandleEngineFault(message));

            try
            {
                _hook = new GlobalKeyboardHook(_settings.IgnoreHeldKeyRepeats);
                _hook.KeyPressed += () => _engine?.Trigger();
            }
            catch (Exception ex)
            {
                ShutdownHardware();
                SetStatus($"Keyboard hook unavailable: {ex.Message}", true);
                return false;
            }

            _testButton.Enabled = true;
            if (WantsFlashing())
            {
                if (!_engine.TryEnable(out var message))
                {
                    SetStatus($"Connection pending: {message}", true);
                    return false;
                }
                SetStatus("Flash active", false);
            }
            else
            {
                SetStatus(diagnostic, false);
            }

            return true;
        }
        finally
        {
            _initializing = false;
        }
    }

    private void ShutdownHardware()
    {
        _hook?.Dispose();
        _hook = null;

        if (_engine is not null)
        {
            _engine.Dispose();
            _engine = null;
            _backlight = null;
        }
        else
        {
            _backlight?.Dispose();
            _backlight = null;
        }
    }

    private bool WantsFlashing() => _flashCheckbox.Checked && _settings.FlashingEnabled;

    private void FlashCheckboxChanged(object? sender, EventArgs e)
    {
        if (_updatingControls)
            return;

        _settings.FlashingEnabled = _flashCheckbox.Checked;
        SaveSettingsQuietly();

        if (!_flashCheckbox.Checked)
        {
            CancelRecovery();
            _engine?.Disable();
            SetStatus("Flash disabled", false);
            return;
        }

        if (_engine is not null && _engine.TryEnable(out _))
            SetStatus("Flash active", false);
        else
            ScheduleRecovery("Connecting flash engine…", RecoveryDelayMilliseconds);
    }

    private void BehaviorOptionChanged(object? sender, EventArgs e)
    {
        if (_updatingControls)
            return;

        _settings.IgnoreHeldKeyRepeats = _ignoreHeldCheckbox.Checked;
        _settings.StartHidden = _silentStartCheckbox.Checked;
        if (_hook is not null)
            _hook.IgnoreHeldKeyRepeats = _settings.IgnoreHeldKeyRepeats;
        SaveSettingsQuietly();
    }

    private void HardwareOptionChanged(object? sender, EventArgs e)
    {
        if (_updatingControls)
            return;

        _settings.UseEnergyDriver = _energyDriverCheckbox.Checked;
        _settings.AllowWmiFallback = _wmiCheckbox.Checked;
        SaveSettingsQuietly();
        ScheduleRecovery("Switching interface…", 0, true);
    }

    private void StartupCheckboxChanged(object? sender, EventArgs e)
    {
        if (_updatingControls)
            return;

        try
        {
            StartupManager.SetEnabled(_startupCheckbox.Checked);
            SetStatus(_startupCheckbox.Checked ? "Startup enabled" : "Startup disabled", false);
        }
        catch (Exception ex)
        {
            _updatingControls = true;
            _startupCheckbox.Checked = StartupManager.IsEnabled;
            _updatingControls = false;
            SetStatus($"Startup setting failed: {ex.Message}", true);
        }
    }

    private void DurationSliderChanged(object? sender, EventArgs e)
    {
        if (_updatingDuration)
            return;
        _updatingDuration = true;
        _durationValue.Value = _durationSlider.Value;
        _updatingDuration = false;
        SaveDuration(_durationSlider.Value);
    }

    private void DurationValueChanged(object? sender, EventArgs e)
    {
        if (_updatingDuration)
            return;
        _updatingDuration = true;
        _durationSlider.Value = _durationValue.Value;
        _updatingDuration = false;
        SaveDuration(_durationValue.Value);
    }

    private void SaveDuration(int value)
    {
        _settings.PulseMilliseconds = Math.Clamp(value, 15, 120);
        _engine?.UpdateTiming(_settings.PulseMilliseconds, _settings.GapMilliseconds);
        SaveSettingsQuietly();
    }

    private async void TestButtonClicked(object? sender, EventArgs e)
    {
        if (_engine is null)
        {
            ScheduleRecovery("Connecting for test…", 0, true);
            return;
        }

        _testButton.Enabled = false;
        try
        {
            await _engine.TestPulseAsync();
            SetStatus("Test flash sent", false);
        }
        catch (Exception ex)
        {
            SetStatus("Connection lost — recovering…", true);
            ScheduleRecovery(ex.Message, RecoveryDelayMilliseconds);
        }
        finally
        {
            if (!IsDisposed)
                _testButton.Enabled = true;
        }
    }

    private void HandleEngineFault(string message)
    {
        if (IsDisposed || _allowClose)
            return;
        SetStatus("Connection lost — recovering…", true);
        ScheduleRecovery(message, RecoveryDelayMilliseconds);
    }

    private void HealthCheck()
    {
        if (_sessionUnavailable || _initializing || IsDisposed)
            return;

        if (_backlight is null || _engine is null || _hook is null)
        {
            ScheduleRecovery("Restoring keyboard interface…", 0);
            return;
        }

        if (WantsFlashing() && (!_engine.Enabled || !_engine.IsResponsive()))
            ScheduleRecovery("Refreshing keyboard interface…", RecoveryDelayMilliseconds);
    }

    private void ScheduleRecovery(string status, int initialDelay, bool force = false)
    {
        if (_sessionUnavailable || IsDisposed || _allowClose)
            return;
        if (!force && !WantsFlashing() && _backlight is not null)
            return;

        CancelRecovery();
        _recoveryCancellation = new CancellationTokenSource();
        SetStatus(status, false);
        _ = RecoverAsync(initialDelay, _recoveryCancellation.Token);
    }

    private async Task RecoverAsync(int initialDelay, CancellationToken cancellationToken)
    {
        try
        {
            if (initialDelay > 0)
                await Task.Delay(initialDelay, cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !_sessionUnavailable && !IsDisposed)
            {
                if (InitializeHardware())
                    return;
                await Task.Delay(RecoveryRetryMilliseconds, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer recovery request replaced this one.
        }
    }

    private void CancelRecovery()
    {
        _recoveryCancellation?.Cancel();
        _recoveryCancellation?.Dispose();
        _recoveryCancellation = null;
    }

    private void SystemEventsOnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        PostToUi(() =>
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _sessionUnavailable = true;
                CancelRecovery();
                _engine?.Disable();
                SetStatus("Paused while Windows is locked", false);
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _sessionUnavailable = false;
                ScheduleRecovery("Restoring after unlock…", RecoveryDelayMilliseconds, true);
            }
        });
    }

    private void SystemEventsOnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        PostToUi(() =>
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _sessionUnavailable = true;
                CancelRecovery();
                _engine?.Disable();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _sessionUnavailable = false;
                ScheduleRecovery("Restoring after resume…", 2200, true);
            }
        });
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
            return;
        try { BeginInvoke(action); }
        catch (InvalidOperationException) { }
    }

    private void BeginWindowDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        ReleaseCapture();
        _ = SendMessage(Handle, 0x00A1, new IntPtr(0x0002), IntPtr.Zero);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OpenGitHubProfile()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Daafir-Rifaad",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            SetStatus($"Could not open GitHub: {exception.Message}", true);
        }
    }

    private void ExitApplication()
    {
        _allowClose = true;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.SessionSwitch -= SystemEventsOnSessionSwitch;
            SystemEvents.PowerModeChanged -= SystemEventsOnPowerModeChanged;
            CancelRecovery();
            _healthTimer.Stop();
            _startupTimer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _toolTip.Dispose();
            _githubImage.Dispose();
            _appMarkImage.Dispose();
            ShutdownHardware();
            _healthTimer.Dispose();
            _startupTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = isError ? AuroraPalette.Danger : AuroraPalette.Muted;
        _trayIcon.Text = text.Length <= 63 ? text : "LOQ KeyFlash";
    }

    private void SaveSettingsQuietly()
    {
        try { _settings.Save(); } catch { }
    }

    private static AuroraPanel CreateSection(string title, Rectangle bounds)
    {
        var panel = new AuroraPanel { Bounds = bounds };
        panel.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(24, 16),
            AutoSize = true,
            ForeColor = AuroraPalette.Accent,
            BackColor = Color.Transparent,
            Font = new Font("Space Grotesk Medium", 9.2f, FontStyle.Bold)
        });
        return panel;
    }

    private static AuroraCheckBox CreateCheckBox(string text, Rectangle bounds, bool isChecked) => new()
    {
        Text = text,
        Bounds = bounds,
        Checked = isChecked
    };

    private static Label CreateFieldLabel(string text, Point location) => new()
    {
        Text = text,
        Location = location,
        AutoSize = true,
        ForeColor = AuroraPalette.Muted,
        BackColor = Color.Transparent,
        Font = new Font("Space Grotesk", 9f)
    };

    private static AuroraButton CreateButton(string text, Rectangle bounds) => new()
    {
        Text = text,
        Bounds = bounds
    };

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
