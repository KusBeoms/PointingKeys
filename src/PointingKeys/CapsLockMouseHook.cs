using System.Runtime.InteropServices;

namespace PointingKeys;

internal enum CapsLockActivationMode
{
    Momentary, // hold CapsLock down to stay active; release to stop
    Toggle,    // press CapsLock once to turn on, press again to turn off
}

/// <summary>
/// Low-level keyboard hook that turns CapsLock into a held modifier (its normal toggle
/// behavior is always suppressed while held) and intercepts a fixed set of action keys
/// (movement, boost, click, scroll) only while CapsLock is active. When not active, those
/// keys pass through untouched so normal typing keeps working.
///
/// A key that started being swallowed (pressed while active) keeps being swallowed -
/// including its eventual keyup and any auto-repeat in between - even if CapsLock
/// deactivates first. Without this, deactivating before the action key releases leaks the
/// key's repeat/keyup into whatever has focus.
///
/// The CapsLock LED tracks whether the modifier is currently active (on/off), as a live
/// "PointingKeys is actively grabbing this key" indicator. We toggle it ourselves via a
/// synthetic keybd_event, which re-enters this same hook; the LLKHF_INJECTED flag tells
/// that synthetic event apart from a real physical press so it can pass straight through
/// to the OS (to actually move the LED) instead of being treated as another press.
/// </summary>
internal sealed class CapsLockMouseHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const int LLKHF_INJECTED = 0x10;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static readonly HashSet<int> WatchedKeys = new()
    {
        VirtualKeys.I, VirtualKeys.J, VirtualKeys.K, VirtualKeys.L,
        VirtualKeys.Q, VirtualKeys.S, VirtualKeys.W,
        VirtualKeys.Three, VirtualKeys.Space, VirtualKeys.E, VirtualKeys.D,
        VirtualKeys.Four, VirtualKeys.R, VirtualKeys.F, VirtualKeys.LeftAlt, VirtualKeys.RightAlt,
        VirtualKeys.N, VirtualKeys.H,
    };

    public event Action<bool>? CapsLockModeChanged;
    public event Action<int, bool>? KeyChanged;

    public CapsLockActivationMode Mode { get; private set; } = CapsLockActivationMode.Momentary;

    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookId;
    private readonly HashSet<int> _swallowedKeys = new();
    private bool _capsHeld;

    public CapsLockMouseHook()
    {
        SetCapsLockLed(false); // start from a known state; hook isn't installed yet, so this reaches the OS normally

        _proc = HookCallback;

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, _proc, NativeMethods.GetModuleHandle(curModule.ModuleName!), 0);

        if (_hookId == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Logger.Log($"SetWindowsHookEx FAILED, error={error}");
            throw new InvalidOperationException($"키보드 후크 설치에 실패했습니다 (오류 코드 {error}).");
        }
    }

    // Switching modes mid-hold would otherwise leave state stuck (e.g. active forever if
    // switched away from Toggle while on), so always deactivate first.
    public void SetMode(CapsLockActivationMode mode)
    {
        if (Mode == mode)
            return;

        if (_capsHeld)
            Deactivate();

        Mode = mode;
    }

    private void Activate()
    {
        _capsHeld = true;
        SetCapsLockLed(true);
        CapsLockModeChanged?.Invoke(true);
    }

    private void Deactivate()
    {
        _capsHeld = false;
        SetCapsLockLed(false);
        CapsLockModeChanged?.Invoke(false);

        // Stop movement/scroll/boost, but deliberately do NOT clear _swallowedKeys: any
        // action key still physically down must keep being swallowed (repeats and its
        // eventual keyup) so it never leaks into the focused app.
        foreach (int downKey in _swallowedKeys)
            KeyChanged?.Invoke(downKey, false);
    }

    private static void SetCapsLockLed(bool on)
    {
        bool currentlyOn = (NativeMethods.GetKeyState(VirtualKeys.CapsLock) & 1) != 0;
        if (currentlyOn == on)
            return;

        NativeMethods.keybd_event((byte)VirtualKeys.CapsLock, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event((byte)VirtualKeys.CapsLock, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                int vkCode = Marshal.ReadInt32(lParam);
                int flags = Marshal.ReadInt32(lParam, 8);
                bool isInjected = (flags & LLKHF_INJECTED) != 0;
                bool isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
                bool isUp = msg is WM_KEYUP or WM_SYSKEYUP;

                if (vkCode == VirtualKeys.CapsLock && (isDown || isUp))
                {
                    if (isInjected)
                    {
                        // Our own SetCapsLockLed() call looping back through this hook: let it
                        // through untouched so it actually reaches the OS and moves the LED.
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    if (Mode == CapsLockActivationMode.Momentary)
                    {
                        if (isDown && !_capsHeld)
                            Activate();
                        else if (isUp && _capsHeld)
                            Deactivate();
                    }
                    else // Toggle: only keydown flips state; keyup is just suppressed
                    {
                        if (isDown)
                        {
                            if (_capsHeld)
                                Deactivate();
                            else
                                Activate();
                        }
                    }

                    return (IntPtr)1; // real CapsLock presses never reach the OS: toggle stays suppressed
                }

                if (WatchedKeys.Contains(vkCode) && (isDown || isUp))
                {
                    if (isDown && _capsHeld)
                    {
                        if (_swallowedKeys.Add(vkCode))
                            KeyChanged?.Invoke(vkCode, true);
                        return (IntPtr)1;
                    }

                    if (isUp && _swallowedKeys.Remove(vkCode))
                    {
                        if (_capsHeld)
                            KeyChanged?.Invoke(vkCode, false);
                        return (IntPtr)1;
                    }

                    if (isDown && _swallowedKeys.Contains(vkCode))
                        return (IntPtr)1; // auto-repeat of a key already swallowed pre-deactivation
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"EXCEPTION in HookCallback: {ex}");
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId == IntPtr.Zero)
            return;

        NativeMethods.UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;

        SetCapsLockLed(false); // hook is gone now, so this reaches the OS normally too; safety net if we exited mid-hold
    }
}
