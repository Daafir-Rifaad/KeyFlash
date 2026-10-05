using LoqKeyFlash.Backlight;

namespace LoqKeyFlash;

internal sealed class FlashEngine : IDisposable
{
    private const int MaxPendingFlashes = 3;
    private readonly IKeyboardBacklight _backlight;
    private readonly SemaphoreSlim _signal = new(0, MaxPendingFlashes);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _stateLock = new();
    private readonly Task _worker;
    private volatile bool _enabled;
    private int _pulseMilliseconds;
    private int _gapMilliseconds;
    private readonly object _hardwareLock = new();

    public event Action<string>? Faulted;
    public bool Enabled => _enabled;

    public FlashEngine(IKeyboardBacklight backlight, int pulseMilliseconds, int gapMilliseconds)
    {
        _backlight = backlight;
        _pulseMilliseconds = pulseMilliseconds;
        _gapMilliseconds = gapMilliseconds;
        _worker = Task.Run(WorkerAsync);
    }

    public bool TryEnable(out string message)
    {
        lock (_stateLock)
        {
            try
            {
                lock (_hardwareLock)
                    _backlight.SetLevel(BacklightLevel.Off);
                _enabled = true;
                message = "Ready — flash is active.";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }

    public void Disable()
    {
        _enabled = false;
        while (_signal.Wait(0)) { }
        TrySetOff();
    }

    public void UpdateTiming(int pulseMilliseconds, int gapMilliseconds)
    {
        _pulseMilliseconds = Math.Clamp(pulseMilliseconds, 15, 120);
        _gapMilliseconds = Math.Clamp(gapMilliseconds, 5, 60);
    }

    public void Trigger()
    {
        if (!_enabled || _signal.CurrentCount >= MaxPendingFlashes)
            return;

        try { _signal.Release(); }
        catch (SemaphoreFullException) { }
    }

    public async Task TestPulseAsync()
    {
        if (_enabled)
        {
            Trigger();
            return;
        }

        await Task.Run(async () =>
        {
            lock (_hardwareLock)
                _backlight.SetLevel(BacklightLevel.High);
            await Task.Delay(_pulseMilliseconds);
            lock (_hardwareLock)
                _backlight.SetLevel(BacklightLevel.Off);
        });
    }

    public bool IsResponsive()
    {
        try
        {
            lock (_hardwareLock)
                _ = _backlight.GetLevel();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task WorkerAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(_shutdown.Token);
                if (!_enabled)
                    continue;

                lock (_hardwareLock)
                    _backlight.SetLevel(BacklightLevel.High);
                await Task.Delay(_pulseMilliseconds, _shutdown.Token);
                lock (_hardwareLock)
                    _backlight.SetLevel(BacklightLevel.Off);
                await Task.Delay(_gapMilliseconds, _shutdown.Token);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _enabled = false;
                TrySetOff();
                Faulted?.Invoke(ex.Message);
            }
        }

        TrySetOff();
    }

    private void TrySetOff()
    {
        try
        {
            lock (_hardwareLock)
                _backlight.SetLevel(BacklightLevel.Off);
        }
        catch { }
    }

    public void Dispose()
    {
        _enabled = false;
        _shutdown.Cancel();
        try { _worker.Wait(750); } catch { }
        TrySetOff();
        _shutdown.Dispose();
        _signal.Dispose();
        _backlight.Dispose();
    }
}
