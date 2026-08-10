namespace PointingKeys;

// Temporary diagnostic logging to pin down where the CapsLock+IJKL pipeline breaks on a
// real keyboard when it doesn't reproduce under synthetic (keybd_event) input.
internal static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
    private static readonly object Lock = new();

    public static void Log(string message)
    {
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostic logging must never crash the app.
        }
    }
}
