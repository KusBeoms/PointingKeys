using System.Diagnostics;
using System.Windows.Forms;

namespace PointingKeys;

internal enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// Moves the mouse cursor continuously while any direction is "held", at a constant speed
/// (base, or boosted while a boost key is held - see SpeedState). Diagonal movement is
/// speed-normalized so holding two perpendicular directions isn't faster than holding one.
/// </summary>
internal sealed class MouseMover : IDisposable
{
    private const int TickIntervalMs = 15;
    private const double BaseSpeedPxPerSec = 350.0;
    private const double BoostSpeedPxPerSec = 1400.0;

    private readonly SpeedState _speedState;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Dictionary<Direction, bool> _held = new()
    {
        [Direction.Up] = false,
        [Direction.Down] = false,
        [Direction.Left] = false,
        [Direction.Right] = false,
    };

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastTickMs;

    public MouseMover(SpeedState speedState)
    {
        _speedState = speedState;
        _timer = new System.Windows.Forms.Timer { Interval = TickIntervalMs };
        _timer.Tick += (_, _) => Tick();
    }

    public void SetDirection(Direction direction, bool isDown)
    {
        _held[direction] = isDown;

        if (_held.Values.Any(v => v))
        {
            if (!_timer.Enabled)
            {
                _lastTickMs = _stopwatch.ElapsedMilliseconds;
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
        }
    }

    public void StopAll()
    {
        foreach (var direction in _held.Keys.ToArray())
            _held[direction] = false;

        _timer.Stop();
    }

    private void Tick()
    {
        double dx = 0, dy = 0;
        if (_held[Direction.Up]) dy -= 1;
        if (_held[Direction.Down]) dy += 1;
        if (_held[Direction.Left]) dx -= 1;
        if (_held[Direction.Right]) dx += 1;

        if (dx == 0 && dy == 0)
            return;

        if (dx != 0 && dy != 0)
        {
            dx *= 0.70710678; // normalize diagonal speed to match axis-aligned speed
            dy *= 0.70710678;
        }

        long nowMs = _stopwatch.ElapsedMilliseconds;
        // WM_TIMER coalesces under load, so the real gap between ticks can run well past the
        // nominal interval; measure it instead of assuming TickIntervalMs, clamped so a stall
        // doesn't produce one giant jump.
        long deltaMs = Math.Clamp(nowMs - _lastTickMs, 0, 100);
        _lastTickMs = nowMs;

        double speedPxPerSec = _speedState.Boosted ? BoostSpeedPxPerSec : BaseSpeedPxPerSec;
        double pxThisTick = speedPxPerSec * deltaMs / 1000.0;

        if (!NativeMethods.GetCursorPos(out var pos))
            return;

        var bounds = SystemInformation.VirtualScreen;
        int newX = Math.Clamp(pos.X + (int)Math.Round(dx * pxThisTick), bounds.Left, bounds.Right - 1);
        int newY = Math.Clamp(pos.Y + (int)Math.Round(dy * pxThisTick), bounds.Top, bounds.Bottom - 1);

        NativeMethods.SetCursorPos(newX, newY);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
