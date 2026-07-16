using Microsoft.Win32;

namespace GamepadServer;

static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Register(string appName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(appName, $"\"{Environment.ProcessPath}\"");
    }

    public static void Unregister(string appName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(appName, throwOnMissingValue: false);
    }
}
