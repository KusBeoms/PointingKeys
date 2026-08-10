using System.Diagnostics;
using System.Windows.Forms;

namespace PointingKeys;

internal enum ScrollDirection
{
    Up,
    Down,
}

/// <summary>
/// Sends discrete wheel notches while a scroll direction is "held", at a constant rate
/// (base, or boosted while a boost key is held - see SpeedState).
/// </summary>
internal sealed class Scroller : IDisposable
{
    private const int TickIntervalMs = 15;
    private const double BaseNotchesPerSec = 8.0;
    private const double BoostNotchesPerSec = 30.0;
    private const int WheelDelta = 120;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;

    private readonly SpeedState _speedState;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<ScrollDirection, bool> _held = new()
    {
        [ScrollDirection.Up] = false,
        [ScrollDirection.Down] = false,
    };

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastTickMs;
    private double _accumulatedNotches;

    public Scroller(SpeedState speedState)
    {
        _speedState = speedState;
        _timer = new System.Windows.Forms.Timer { Interval = TickIntervalMs };
        _timer.Tick += (_, _) => Tick();
    }

    public void SetDirection(ScrollDirection direction, bool isDown)
    {
        bool wasAnyHeld = _held.Values.Any(v => v);
        _held[direction] = isDown;

        if (isDown && !wasAnyHeld)
        {
            // A tap shorter than one notch-interval would otherwise accumulate a fraction
            // that gets thrown away on release and never scroll at all; a short tap should
            // always move exactly one notch (same idea as AcceleratingHotkeyManager).
            SendNotch(direction);
            _lastTickMs = _stopwatch.ElapsedMilliseconds;
            _accumulatedNotches = 0;
            _timer.Start();
        }
        else if (!_held.Values.Any(v => v))
        {
            _timer.Stop();
        }
    }

    private static void SendNotch(ScrollDirection direction)
    {
        int sign = direction == ScrollDirection.Up ? 1 : -1;
        NativeMethods.mouse_event(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)(sign * WheelDelta)), UIntPtr.Zero);
    }

    public void StopAll()
    {
        _held[ScrollDirection.Up] = false;
        _held[ScrollDirection.Down] = false;
        _timer.Stop();
    }

    private void Tick()
    {
        int sign = 0;
        if (_held[ScrollDirection.Up]) sign += 1;
        if (_held[ScrollDirection.Down]) sign -= 1;
        if (sign == 0)
            return;

        long nowMs = _stopwatch.ElapsedMilliseconds;
        long deltaMs = Math.Clamp(nowMs - _lastTickMs, 0, 100);
        _lastTickMs = nowMs;

        double notchesPerSec = _speedState.Boosted ? BoostNotchesPerSec : BaseNotchesPerSec;
        _accumulatedNotches += notchesPerSec * deltaMs / 1000.0;

        while (_accumulatedNotches >= 1.0)
        {
            NativeMethods.mouse_event(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)(sign * WheelDelta)), UIntPtr.Zero);
            _accumulatedNotches -= 1.0;
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
