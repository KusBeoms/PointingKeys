namespace PointingKeys;

// Maps key press/release directly to mouse button down/up (not a single "click"), so
// holding the key down also supports click-and-drag while moving with IJKL.
internal static class MouseButtons
{
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    public static void SetLeftDown(bool isDown) =>
        NativeMethods.mouse_event(isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

    public static void SetRightDown(bool isDown) =>
        NativeMethods.mouse_event(isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
}
