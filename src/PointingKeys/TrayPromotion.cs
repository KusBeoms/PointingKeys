using Microsoft.Win32;

namespace PointingKeys;

internal static class TrayPromotion
{
    /// <summary>
    /// Windows 11 puts new tray icons in the hidden overflow. Explorer records each icon under
    /// HKCU\Control Panel\NotifyIconSettings; setting IsPromoted shows it on the taskbar. Only done while
    /// the user has not chosen (no IsPromoted value yet), so hiding it later sticks.
    /// </summary>
    public static async Task PromoteAsync()
    {
        const string root = @"Control Panel\NotifyIconSettings";
        for (int attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(1500); // Explorer writes the entry shortly after the icon appears
            try
            {
                using var settings = Registry.CurrentUser.OpenSubKey(root);
                if (settings is null) return; // not Windows 11
                foreach (var name in settings.GetSubKeyNames())
                {
                    using var entry = Registry.CurrentUser.OpenSubKey($@"{root}\{name}", writable: true);
                    if (entry?.GetValue("ExecutablePath") is not string exe ||
                        !string.Equals(exe, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (entry.GetValue("IsPromoted") is null) entry.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                    return;
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                Logger.Log($"tray promotion failed: {ex}");
                return;
            }
        }
    }
}
