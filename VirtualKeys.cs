namespace PointingKeys;

// All bindings are only active while CapsLock is held (see CapsLockMouseHook).
internal static class VirtualKeys
{
    public const int CapsLock = 0x14;

    // Movement (WASD-style, using IJKL)
    public const int I = 0x49; // up
    public const int J = 0x4A; // left
    public const int K = 0x4B; // down
    public const int L = 0x4C; // right

    // Speed boost - held together with a movement/scroll key for the fast tier
    public const int Q = 0x51;
    public const int S = 0x53;
    public const int W = 0x57;

    // Left click
    public const int Three = 0x33;
    public const int Space = 0x20;
    public const int E = 0x45;
    public const int D = 0x44;

    // Right click
    public const int Four = 0x34;
    public const int R = 0x52;
    public const int F = 0x46;
    public const int LeftAlt = 0xA4;
    public const int RightAlt = 0xA5;

    // Scroll
    public const int N = 0x4E; // down
    public const int H = 0x48; // up
}
