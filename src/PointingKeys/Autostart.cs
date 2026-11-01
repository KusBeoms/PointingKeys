using Microsoft.Win32;

namespace PointingKeys;

/// <summary>HKCU Run entry so PointingKeys starts with Windows.</summary>
internal static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PointingKeys";

    public static bool IsEnabled
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(ValueName) is string s && s.Contains(Environment.ProcessPath!, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) k.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else if (k.GetValue(ValueName) is not null) k.DeleteValue(ValueName);
        }
        catch (Exception ex) { Logger.Log($"autostart update failed: {ex}"); }
    }
}
